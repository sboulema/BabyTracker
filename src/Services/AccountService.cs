using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using PostmarkDotNet;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;
using BabyTracker.Models.Account;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public interface IAccountService
{
    Task<ClaimsPrincipal?> Login(LoginViewModel model);

    Task<SignupUserResponse?> Register(LoginViewModel model);

    Task<UserMetaData?> GetUserMetaData(ClaimsPrincipal user);

    Task<bool> SaveUserMetaData(ClaimsPrincipal user, UserMetaData userMetaData);

    Task<List<UserResponseSchema>?> SearchUsersWithEnableMemoriesEmail();

    Task<string> ResetPassword(LoginViewModel model);

    Task<bool> RemoveAccount(ClaimsPrincipal user);

    Task<string?> GetUserEmail(ClaimsPrincipal user);

    Task<bool> RequestEmailChange(ClaimsPrincipal user, string email, string verificationUrl);

    Task<bool> VerifyEmailChange(string userId, string token);
}

public class AccountService(IConfiguration configuration,
    IAuthenticationApiClient authenticationApiClient,
    IManagementApiClient managementApiClient,
    IWebHostEnvironment webHostEnvironment) : IAccountService
{
    public async Task<ClaimsPrincipal?> Login(LoginViewModel model)
    {
        AccessTokenResponse? result;

        try
        {
            result = await authenticationApiClient.GetTokenAsync(new ResourceOwnerTokenRequest
            {
                ClientId = configuration["AUTH0_CLIENTID"],
                ClientSecret = configuration["AUTH0_CLIENTSECRET"],
                Scope = "openid profile",
                Realm = "Username-Password-Authentication",
                Username = model.EmailAddress,
                Password = model.Password
            });
        }
        catch (Exception)
        {
            return null;
        }

        var user = await authenticationApiClient.GetUserInfoAsync(result.AccessToken);
        var userId = user.UserId.Replace("auth0|", string.Empty);

        var shareUser = await SearchUsersWithShareList(user.FullName);
        var shareUserId = shareUser?.UserId.Replace("auth0|", string.Empty);

        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("nickname", user.NickName),
            new Claim("picture", user.Picture),
            new Claim("userId", userId),
            new Claim("shareUserId", shareUserId ?? string.Empty),
            new Claim("activeUserId", !string.IsNullOrEmpty(shareUserId) ? shareUserId : userId)
        ], CookieAuthenticationDefaults.AuthenticationScheme);

        var claimsPrincipal = new ClaimsPrincipal(identity);

        return claimsPrincipal;
    }

    public async Task<SignupUserResponse?> Register(LoginViewModel model)
    {
        var result = await authenticationApiClient.SignupUserAsync(new SignupUserRequest
        {
            ClientId = configuration["AUTH0_CLIENTID"],
            Email = model.EmailAddress,
            Password = model.Password,
            Connection = "Username-Password-Authentication",
            Name = model.EmailAddress,
        });
        return result;
    }

    public async Task<string> ResetPassword(LoginViewModel model)
    {
        var result = await authenticationApiClient.ChangePasswordAsync(new()
        {
            ClientId = configuration["AUTH0_CLIENTID"],
            Connection = "Username-Password-Authentication",
            Email = model.EmailAddress
        });
        return result;
    }

    public async Task<string?> GetUserEmail(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var clientUser = await managementApiClient.Users.GetAsync(userId, new());
        return clientUser.Email;
    }

    public async Task<bool> RequestEmailChange(ClaimsPrincipal user, string email, string verificationUrl)
    {
        if (string.IsNullOrWhiteSpace(email) || !MailAddress.TryCreate(email, out var address))
        {
            return false;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return false;
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var metadata = await GetUserMetaData(user) ?? new UserMetaData();
        metadata.PendingEmailAddress = address.Address;
        metadata.EmailChangeTokenHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        metadata.EmailChangeTokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);

        if (!await SaveUserMetaData(user, metadata))
        {
            return false;
        }

        var message = new PostmarkMessage
        {
            To = address.Address,
            From = $"{configuration["MEMORIES_FROM_NAME"]} <{configuration["MEMORIES_FROM_EMAIL"]}>",
            Subject = "BabyTracker - Verify your new email address",
            TextBody = $"To confirm your new BabyTracker email address, open this link within one hour:

{verificationUrl}?userId={Uri.EscapeDataString(userId)}&token={Uri.EscapeDataString(token)}

If you did not request this change, ignore this email."
        };

        var result = await new PostmarkClient(configuration["POSTMARK_API_TOKEN"]).SendMessageAsync(message);
        return result.Status == PostmarkStatus.Success;
    }

    public async Task<bool> VerifyEmailChange(string userId, string token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var clientUser = await managementApiClient.Users.GetAsync(userId, new());
        var metadata = GetUserMetaData(clientUser);
        if (metadata?.PendingEmailAddress is not { Length: > 0 } pendingEmail ||
            string.IsNullOrEmpty(metadata.EmailChangeTokenHash) ||
            metadata.EmailChangeTokenExpiresAt <= DateTimeOffset.UtcNow)
        {
            return false;
        }

        var suppliedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(suppliedHash),
            Encoding.UTF8.GetBytes(metadata.EmailChangeTokenHash)))
        {
            return false;
        }

        metadata.PendingEmailAddress = null;
        metadata.EmailChangeTokenHash = null;
        metadata.EmailChangeTokenExpiresAt = null;
        var json = JsonSerializer.Serialize(metadata);
        var userMetadata = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

        await managementApiClient.Users.UpdateAsync(userId, new()
        {
            Email = pendingEmail,
            EmailVerified = true,
            UserMetadata = userMetadata
        });

        return true;
    }

    public async Task<bool> RemoveAccount(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var activeUserId = user.FindFirstValue("activeUserId");

        try
        {
            await managementApiClient.Users.DeleteAsync(userId);
            
            RemoveDataCloneDirectory(activeUserId);
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void RemoveDataCloneDirectory(string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return;
        }

        var path = $"/data/Data/{userId}";

        if (!webHostEnvironment.IsProduction())
        {
            path = Path.Combine(webHostEnvironment.ContentRootPath, "Data", userId);
        }

        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Error removing data clone directory '{path}'. {e.Message}");
        }
    }

    public async Task<UserMetaData?> GetUserMetaData(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var clientUser = await managementApiClient.Users.GetAsync(userId, new());

        return GetUserMetaData(clientUser);
    }

    public static UserMetaData? GetUserMetaData(GetUserResponseContent user)
    {
        var json = JsonSerializer.Serialize(user.UserMetadata);
        return JsonSerializer.Deserialize<UserMetaData>(json);
    }

    public static UserMetaData? GetUserMetaData(UserResponseSchema user)
    {
        var json = JsonSerializer.Serialize(user.UserMetadata);
        return JsonSerializer.Deserialize<UserMetaData>(json);
    }

    public async Task<bool> SaveUserMetaData(ClaimsPrincipal user, UserMetaData userMetaData)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var json = JsonSerializer.Serialize(userMetaData);
        var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);

        await managementApiClient.Users.UpdateAsync(userId, new()
        {
            UserMetadata = dict
        });

        return true;
    }

    public async Task<List<UserResponseSchema>?> SearchUsersWithEnableMemoriesEmail()
        => await QueryUsers("user_metadata.EnableMemoriesEmail:true");

    private async Task<UserResponseSchema?> SearchUsersWithShareList(string emailAddress)
    {
        var users = await QueryUsers($"user_metadata.ShareList:{emailAddress}");
        return users?.FirstOrDefault();
    }

    private async Task<List<UserResponseSchema>?> QueryUsers(string query)
    {
        var users = new List<UserResponseSchema>();

        var request = new ListUsersRequestParameters
        {
            Q = query
        };

        var pager = await managementApiClient.Users.ListAsync(request);

        await foreach (var user in pager)
        {
            users.Add(user);
        }

        return users;
    }
}
