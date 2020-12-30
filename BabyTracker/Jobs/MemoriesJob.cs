using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BabyTracker.Models;
using BabyTracker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using SendGrid;
using SendGrid.Helpers.Mail;

[DisallowConcurrentExecution]
public class MemoriesJob : IJob
{
    private readonly ILogger<MemoriesJob> _logger;
    private readonly ISqLiteService _sqLiteService;
    private readonly IConfiguration _configuration;
    private readonly ISendGridClient _sendGridClient;

    public MemoriesJob(ILogger<MemoriesJob> logger, ISqLiteService sqLiteService,
        IConfiguration configuration, ISendGridClient sendGridClient)
    {
        _logger = logger;
        _sqLiteService = sqLiteService;
        _configuration = configuration;
        _sendGridClient = sendGridClient;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var babyNames = _configuration["MEMORIES_NAMES"].Split(";");

        foreach (var babyName in babyNames)
        {
            var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

            _logger.LogInformation($"Found {memories.Count} memories for {babyName}");

            if (memories.Count > 0) 
            {
                await SendEmail(memories, babyName);
            }            
        }
    }

    private async Task<Response> SendEmail(List<EntryModel> memories, string babyName)
    {
        var msg = new SendGridMessage()
        {
            From = new EmailAddress(_configuration["MEMORIES_FROM_EMAIL"], _configuration["MEMORIES_FROM_NAME"]),
            Subject = $"BabyTracker - Memories {DateTime.Now.ToString("yyyy-MM-dd")}"
        };
        msg.AddContent(MimeType.Text, $"Found {memories.Count} memories for {babyName}");

        var recipients = _configuration[$"MEMORIES_{babyName.ToUpper()}_TO"].Split(";");

        foreach (var recipient in recipients)
        {
            msg.AddTo(new EmailAddress(recipient));
        }
        
        return await _sendGridClient.SendEmailAsync(msg).ConfigureAwait(false);
    } 
}