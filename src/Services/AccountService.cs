using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Configuration;

namespace BabyTracker.Services;

public interface IAccountService
{
    Task<ClaimsPrincipal?> Login(LoginViewModel model);

    Task<SignupUserResponse?> Register(LoginViewModel model);

    Task<UserMetaData?> GetUserMetaData(ClaimsPrincipal user);

    Task<bool> SaveUserMetaData(ClaimsPrincipal user, UserMetaData userMetaData);

    Task<List<UserResponseSchema>?> SearchUsersWithEnableMemoriesEmail();

    Task<string> ResetPassword(LoginViewModel model);
}

public class AccountService(IConfiguration configuration,
    IAuthenticationApiClient authenticationApiClient,
    IManagementApiClient managementApiClient) : IAccountService
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
