using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Auth0.ManagementApi.Models;
using BabyTracker.Models;
using BabyTracker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace BabyTracker.Jobs;

[DisallowConcurrentExecution]
public class MemoriesJob : IJob
{
    private readonly ILogger<MemoriesJob> _logger;
    private readonly ISqLiteService _sqLiteService;
    private readonly IConfiguration _configuration;
    private readonly ISendGridClient _sendGridClient;
    private readonly IMemoriesService _memoriesService;
    private readonly IAccountService _accountService;

    public MemoriesJob(ILogger<MemoriesJob> logger, ISqLiteService sqLiteService,
        IConfiguration configuration, ISendGridClient sendGridClient,
        IMemoriesService memoriesService, IAccountService accountService)
    {
        _logger = logger;
        _sqLiteService = sqLiteService;
        _configuration = configuration;
        _sendGridClient = sendGridClient;
        _memoriesService = memoriesService;
        _accountService = accountService;
    }

    public async Task Execute(IJobExecutionContext context)
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

    private async Task<Response?> SendEmail(List<EntryModel> memories, User user, string babyName)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_configuration["MEMORIES_FROM_EMAIL"], _configuration["MEMORIES_FROM_NAME"]),
            Subject = $"BabyTracker - Memories {DateTime.Now.ToShortDateString()}"
        };

        var mjml = await _memoriesService.GetMJML(memories, babyName);
        var html = await _memoriesService.GetHTML(mjml);

        msg.AddContent(MimeType.Html, html);

        var userMetaData = AccountService.GetUserMetaData(user);

        var recipients = userMetaData?.MemoriesAddresses.Split(";");

        if (recipients?.Any() != true)
        {
            return null;
        }

        foreach (var recipient in recipients)
        {
            msg.AddTo(new EmailAddress(recipient));
        }

        return await _sendGridClient.SendEmailAsync(msg).ConfigureAwait(false);
    }
}
