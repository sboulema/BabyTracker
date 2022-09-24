using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using BabyTracker.Models;
using BabyTracker.Models.Account;
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

public class MemoriesService : IMemoriesService
{
    private readonly IConfiguration _configuration;
    private readonly IAccountService _accountService;
    private readonly ISqLiteService _sqLiteService;
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<MemoriesService> _logger;

    public MemoriesService(IConfiguration configuration,
        IAccountService accountService,
        ISqLiteService sqLiteService,
        ISendGridClient sendGridClient,
        ILogger<MemoriesService> logger)
    {
        _configuration = configuration;
        _accountService = accountService;
        _sqLiteService = sqLiteService;
        _sendGridClient = sendGridClient;
        _logger = logger;
    }

    public async Task SendMemoriesEmail()
    {
        var users = await _accountService.SearchUsersWithEnableMemoriesEmail();

        if (users?.Any() != true)
        {
            return;
        }

        foreach (var user in users)
        {
            var shortUserId = AccountService.GetUserId(user.UserId);

            var babies = _sqLiteService.GetBabiesFromDb(shortUserId);

            foreach (var baby in babies)
            {
                var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, shortUserId, baby.BabyName);

                _logger.LogInformation($"Found {memories.Count} memories for {baby.BabyName}");

                if (memories.Count > 0)
                {
                    await SendEmail(memories, user, shortUserId, baby.BabyName);
                }
            }
        }
    }

    private async Task<string> GetMJML(List<EntryModel> memories, string userId, string babyName)
    {
        var importResultModel = new ImportResultModel
        {
            Entries = memories
        };

        var model = DiaryService.GetDays(importResultModel);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;
        model.BaseUrl = _configuration["BASE_URL"];
        model.Profile = new Profile
        {
            UserId = userId
        };

        model.Entries = model.Entries
            .OrderByDescending(entry => entry.TimeUTC.Year)
            .OrderBy(entry => entry.TimeUTC.TimeOfDay);

        var mjml = await RazorTemplateEngine.RenderAsync("/Views/Emails/MemoriesEmail.cshtml", model);

        return mjml;
    }

    private string GetHTML(string mjml)
    {
        var result = new MjmlRenderer().Render(mjml, new() {
            Beautify = false
        });

        return result.Html;
    }

    private async Task<Response?> SendEmail(List<EntryModel> memories, User user, string userId, string babyName)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_configuration["MEMORIES_FROM_EMAIL"], _configuration["MEMORIES_FROM_NAME"]),
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

        var response = await _sendGridClient.SendEmailAsync(msg);

        return response;
    }
}
