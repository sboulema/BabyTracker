using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mjml.Net;
using Razor.Templating.Core;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BabyTracker.Services;

public interface IMemoriesService
{
    Task SendMemoriesEmail();
}

public class MemoriesService(IConfiguration configuration,
    IAccountService accountService,
    ISqLiteService sqLiteService,
    ISendGridClient sendGridClient,
    ILogger<MemoriesService> logger) : IMemoriesService
{
    public async Task SendMemoriesEmail()
    {
        var users = await accountService.SearchUsersWithEnableMemoriesEmail();

        if (users?.Count == 0)
        {
            return;
        }

        foreach (var user in users)
        {
            var userId = user.UserId.Replace("auth0|", string.Empty);

            sqLiteService.OpenDataConnection(userId);

            var babies = await sqLiteService.GetBabiesFromDb();

            foreach (var baby in babies)
            {
                var memories = await sqLiteService.GetMemoriesFromDb(DateTime.Now, baby.Name);

                logger.LogInformation($"Found {memories.Count} memories for {baby.Name}");

                if (memories.Count > 0)
                {
                    await SendEmail(memories, user, userId, baby.Name);
                }
            }

            await sqLiteService.CloseDataConnection();
        }
    }

    private async Task<string> GetMJML(List<IMemoryEntry> memories, string userId, string babyName)
    {
        var model = new MemoriesEmailViewModel
        {
            BabyName = babyName,
            BaseUrl = configuration["BASE_URL"] ?? string.Empty,
            UserId = userId,
            Entries = memories
                .OrderByDescending(entry => entry.Time.ToDateTimeUTC().Year)
                .ThenBy(entry => entry.Time.ToDateTimeUTC().TimeOfDay)
                .GroupBy(entry => entry.Time.ToDateTimeUTC().Year)
                .OrderBy(entry => entry.Key)
        };

        var mjml = await RazorTemplateEngine.RenderAsync("/Views/Emails/MemoriesEmail.cshtml", model);

        return mjml;
    }

    private static string GetHTML(string mjml)
    {
        var result = new MjmlRenderer().Render(mjml, new() {
            Beautify = false
        });

        return result.Html;
    }

    private async Task<Response?> SendEmail(List<IMemoryEntry> memories, User user, string userId, string babyName)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(configuration["MEMORIES_FROM_EMAIL"], configuration["MEMORIES_FROM_NAME"]),
            Subject = $"BabyTracker - Memories {DateTime.Now.ToShortDateString()}"
        };

        var mjml = await GetMJML(memories, userId, babyName);
        var html = GetHTML(mjml);

        msg.AddContent(MimeType.Html, html);

        var userMetaData = AccountService.GetUserMetaData(user);

        var recipients = userMetaData?.MemoriesAddresses.Split(",");

        if (recipients?.Any() != true)
        {
            return null;
        }

        foreach (var recipient in recipients)
        {
            msg.AddTo(new EmailAddress(recipient));
        }

        var response = await sendGridClient.SendEmailAsync(msg);

        return response;
    }
}
