using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;
using Auth0.ManagementApi;
using Auth0.ManagementApi.Models;
using Auth0.ManagementApi.Paging;
using BabyTracker.Models.Account;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace BabyTracker.Services;

public interface IAccountService
{
    Task<ClaimsPrincipal?> Login(LoginViewModel model);

    Task<SignupUserResponse?> Register(LoginViewModel model);

    Task<UserMetaData?> GetUserMetaData(ClaimsPrincipal user);

    Task SaveUserMetaData(ClaimsPrincipal user, UserMetaData userMetaData);

    Task<List<User>?> SearchUsersWithEnableMemoriesEmail();

    Task<Profile?> GetProfile(ClaimsPrincipal user);

    Task<string> ResetPassword(LoginViewModel model);
}

public class AccountService : IAccountService
{
    private readonly IConfiguration _configuration;
    private readonly AuthenticationApiClient _authenticationApiClient;

    public AccountService(IConfiguration configuration,
        AuthenticationApiClient authenticationApiClient)
    {
        _configuration = configuration;
        _authenticationApiClient = authenticationApiClient;
    }

    public async Task<ClaimsPrincipal?> Login(LoginViewModel model)
    {
        AccessTokenResponse? result;

        try
        {
            result = await _authenticationApiClient.GetTokenAsync(new ResourceOwnerTokenRequest
            {
                ClientId = _configuration["AUTH0_CLIENTID"],
                ClientSecret = _configuration["AUTH0_CLIENTSECRET"],
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

        var user = await _authenticationApiClient.GetUserInfoAsync(result.AccessToken);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("nickname", user.NickName),
            new Claim("picture", user.Picture),
            new Claim("userId", await GetUserId(user.UserId, user.FullName))
        }, CookieAuthenticationDefaults.AuthenticationScheme);

        var claimsPrincipal = new ClaimsPrincipal(identity);

        return claimsPrincipal;
    }

    public async Task<SignupUserResponse?> Register(LoginViewModel model)
    {
        var result = await _authenticationApiClient.SignupUserAsync(new SignupUserRequest
        {
            ClientId = _configuration["AUTH0_CLIENTID"],
            Email = model.EmailAddress,
            Password = model.Password,
            Connection = "Username-Password-Authentication",
            Name = model.EmailAddress,
        });
        return result;
    }

    public async Task<string> ResetPassword(LoginViewModel model)
    {
        var result = await _authenticationApiClient.ChangePasswordAsync(new()
        {
            ClientId = _configuration["AUTH0_CLIENTID"],
            Connection = "Username-Password-Authentication",
            Email = model.EmailAddress
        });
        return result;
    }

    public async Task<Profile?> GetProfile(ClaimsPrincipal user)
    {
        if (user == null)
        {
            return null;
        }

        return new()
        {
            Name = user.Identity?.Name ?? string.Empty,
            Nickname = user.FindFirst("nickname")?.Value ?? string.Empty,
            Image = user.FindFirst("picture")?.Value ?? string.Empty,
            UserId = await GetUserId(user)
        };
    }

    public async Task<UserMetaData?> GetUserMetaData(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var client = new ManagementApiClient(await GetAccessToken(), _configuration["AUTH0_DOMAIN"]);
        var clientUser = await client.Users.GetAsync(userId);

        return GetUserMetaData(clientUser);
    }

    public static UserMetaData? GetUserMetaData(User user)
    {
        if (user.UserMetadata is not JObject userMetaDataObject)
        {
            return null;
        }

        return userMetaDataObject.ToObject<UserMetaData>();
    }

    public async Task SaveUserMetaData(ClaimsPrincipal user, UserMetaData userMetaData)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var client = new ManagementApiClient(await GetAccessToken(), _configuration["AUTH0_DOMAIN"]);

        await client.Users.UpdateAsync(userId, new()
        {
            UserMetadata = userMetaData
        });
    }

    public async Task<List<User>?> SearchUsersWithEnableMemoriesEmail()
    {
        var client = new ManagementApiClient(await GetAccessToken(), _configuration["AUTH0_DOMAIN"]);

        var users = new List<User>();

        var pageNo = 0;

        IPagedList<User> page;

        do
        {
            page = await client.Users.GetAllAsync(
                new() { Query = "user_metadata.EnableMemoriesEmail:true" },
                new(pageNo)
            );

            users.AddRange(page);

            pageNo++;
        } while (page.Paging != null && page.Paging.Length == page.Paging.Limit);

        return users;
    }

    private async Task<User?> SearchUsersWithShareList(string emailAddress)
    {
        var client = new ManagementApiClient(await GetAccessToken(), _configuration["AUTH0_DOMAIN"]);

        var users = new List<User>();

        var pageNo = 0;

        IPagedList<User> page;

        do
        {
            page = await client.Users.GetAllAsync(
                new() { Query = $"user_metadata.ShareList:{emailAddress}" },
                new(pageNo)
            );

            users.AddRange(page);

            pageNo++;
        } while (page.Paging != null && page.Paging.Length == page.Paging.Limit);

        return users.FirstOrDefault();
    }

    public static string GetUserId(string userIdWithIdentifier)
        => userIdWithIdentifier.Replace("auth0|", string.Empty);

    private async Task<string> GetUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var shareUser = await SearchUsersWithShareList(user.FindFirstValue(ClaimTypes.Name));

        if (shareUser != null)
        {
            userId = shareUser.UserId;
        }

        return GetUserId(userId);
    }

    private async Task<string> GetUserId(string userId, string userName)
    {
        var shareUser = await SearchUsersWithShareList(userName);

        if (shareUser != null)
        {
            userId = shareUser.UserId;
        }

        return GetUserId(userId);
    }

    private async Task<string> GetAccessToken()
    {
        var token = await _authenticationApiClient.GetTokenAsync(new ClientCredentialsTokenRequest
        {
            ClientId = _configuration["AUTH0_MACHINE_CLIENTID"],
            ClientSecret = _configuration["AUTH0_MACHINE_CLIENTSECRET"],
            Audience = $"https://{_configuration["AUTH0_DOMAIN"]}/api/v2/"
        });

        return token?.AccessToken ?? string.Empty;
    }
}
