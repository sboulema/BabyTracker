using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using BabyTracker.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mjml.AspNetCore;
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
    private readonly IMjmlServices _mjmlServices;
    private readonly IAccountService _accountService;
    private readonly ISqLiteService _sqLiteService;
    private readonly ISendGridClient _sendGridClient;
    private readonly ILogger<MemoriesService> _logger;

    public MemoriesService(IConfiguration configuration,
        IMjmlServices mjmlServices,
        IAccountService accountService,
        ISqLiteService sqLiteService,
        ISendGridClient sendGridClient,
        ILogger<MemoriesService> logger)
    {
        _configuration = configuration;
        _mjmlServices = mjmlServices;
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
                    await SendEmail(memories, user, baby.BabyName);
                }
            }
        }
    }

    private async Task<string> GetMJML(List<EntryModel> memories, string babyName)
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

        model.Entries = model.Entries
            .OrderByDescending(entry => entry.TimeUTC.Year)
            .OrderBy(entry => entry.TimeUTC.TimeOfDay);

        var mjml = await RazorTemplateEngine.RenderAsync("/Views/Emails/MemoriesEmail.cshtml", model);

        return mjml;
    }

    private async Task<string> GetHTML(string mjml)
    {
        var result = await _mjmlServices.Render(mjml);
        return result.Html;
    }

    private async Task<Response?> SendEmail(List<EntryModel> memories, User user, string babyName)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_configuration["MEMORIES_FROM_EMAIL"], _configuration["MEMORIES_FROM_NAME"]),
            Subject = $"BabyTracker - Memories {DateTime.Now.ToShortDateString()}"
        };

        var mjml = await GetMJML(memories, babyName);
        var html = await GetHTML(mjml);

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
