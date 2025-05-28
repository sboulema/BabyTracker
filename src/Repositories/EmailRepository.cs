using System;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using BabyTracker.Services;
using Microsoft.Extensions.Configuration;
using Mjml.Net;
using PostmarkDotNet;

namespace BabyTracker.Repositories;

public interface IEmailRepository
{
    Task<bool> SendEmail(string mjml, User user, string userId, string babyName);
}

public class EmailRepository(IConfiguration configuration) : IEmailRepository
{
    public async Task<bool> SendEmail(string mjml, User user, string userId, string babyName)
    {
        var userMetaData = AccountService.GetUserMetaData(user);

        if (string.IsNullOrEmpty(userMetaData?.MemoriesAddresses))
        {
            return false;
        }

        var message = new PostmarkMessage
        {
            To = userMetaData.MemoriesAddresses,
            From = $"{configuration["MEMORIES_FROM_NAME"]} <{configuration["MEMORIES_FROM_EMAIL"]}>",
            Subject = $"BabyTracker - Memories {DateTime.Now:dd-MM-yyyy}",
            HtmlBody = GetHTML(mjml),
        };

        var client = new PostmarkClient(configuration["POSTMARK_API_TOKEN"]);
        var sendResult = await client.SendMessageAsync(message);

        return sendResult.Status == PostmarkStatus.Success;
    }

    private static string GetHTML(string mjml)
    {
        var result = new MjmlRenderer().Render(mjml, new()
        {
            Beautify = false
        });

        return result.Html;
    }
}
