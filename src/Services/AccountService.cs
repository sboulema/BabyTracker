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

    Task<bool> SaveUserMetaData(ClaimsPrincipal user, UserMetaData userMetaData);

    Task<List<User>?> SearchUsersWithEnableMemoriesEmail();

    Task<string> ResetPassword(LoginViewModel model);
}

public class AccountService : IAccountService
{
    private readonly IConfiguration _configuration;
    private readonly IAuthenticationApiClient _authenticationApiClient;
    private readonly IManagementApiClient _managementApiClient;

    public AccountService(IConfiguration configuration,
        IAuthenticationApiClient authenticationApiClient,
        IManagementApiClient managementApiClient)
    {
        _configuration = configuration;
        _authenticationApiClient = authenticationApiClient;
        _managementApiClient = managementApiClient;
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
        var userId = user.UserId.Replace("auth0|", string.Empty);

        var shareUser = await SearchUsersWithShareList(user.FullName);
        var shareUserId = shareUser?.UserId.Replace("auth0|", string.Empty);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim("nickname", user.NickName),
            new Claim("picture", user.Picture),
            new Claim("userId", userId),
            new Claim("shareUserId", shareUserId ?? string.Empty),
            new Claim("activeUserId", !string.IsNullOrEmpty(shareUserId) ? shareUserId : userId)
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

    public async Task<UserMetaData?> GetUserMetaData(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var clientUser = await _managementApiClient.Users.GetAsync(userId);

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

    public async Task<bool> SaveUserMetaData(ClaimsPrincipal user, UserMetaData userMetaData)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        await _managementApiClient.Users.UpdateAsync(userId, new()
        {
            UserMetadata = userMetaData
        });

        return true;
    }

    public async Task<List<User>?> SearchUsersWithEnableMemoriesEmail()
    {
        var users = new List<User>();

        var pageNo = 0;

        IPagedList<User> page;

        do
        {
            page = await _managementApiClient.Users.GetAllAsync(
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
        var users = new List<User>();

        var pageNo = 0;

        IPagedList<User> page;

        do
        {
            page = await _managementApiClient.Users.GetAllAsync(
                new() { Query = $"user_metadata.ShareList:{emailAddress}" },
                new(pageNo)
            );

            users.AddRange(page);

            pageNo++;
        } while (page.Paging != null && page.Paging.Length == page.Paging.Limit);

        return users.FirstOrDefault();
    }
}
