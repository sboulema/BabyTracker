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
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace BabyTracker.Services;

public interface IAccountService
{
    Task<UserMetaData?> GetUserMetaData(ClaimsPrincipal user);

    Task SaveUserMetaData(ClaimsPrincipal user, UserMetaData userMetaData);

    Task<List<User>?> SearchUsersWithEnableMemoriesEmail();
}

public class AccountService : IAccountService
{
    private readonly IConfiguration _configuration;

    public AccountService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public static Profile? GetProfile(ClaimsPrincipal user)
    {
        if (user == null)
        {
            return null;
        }

        return new()
        {
            Name = user.Identity?.Name ?? string.Empty,
            Nickname = user.Claims.FirstOrDefault(c => c.Type == "nickname")?.Value ?? string.Empty,
            Image = user.Claims.FirstOrDefault(c => c.Type == "picture")?.Value ?? string.Empty,
            UserId = GetUserId(user)
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
        } while (page.Paging.Length == page.Paging.Limit);

        return users;
    }

    public static string GetUserId(string userIdWithIdentifier)
        => userIdWithIdentifier.Replace("auth0|", string.Empty);

    private static string GetUserId(ClaimsPrincipal user)
    {
        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        return GetUserId(userId);
    }

    private async Task<string> GetAccessToken()
    {
        var client = new AuthenticationApiClient(_configuration["AUTH0_DOMAIN"]);

        var token = await client.GetTokenAsync(new ClientCredentialsTokenRequest
        {
            ClientId = _configuration["AUTH0_MACHINE_CLIENTID"],
            ClientSecret = _configuration["AUTH0_MACHINE_CLIENTSECRET"],
            Audience = $"https://{_configuration["AUTH0_DOMAIN"]}/api/v2/"
        });

        return token?.AccessToken ?? string.Empty;
    }
}
