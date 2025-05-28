using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using BabyTracker.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Razor.Templating.Core;

namespace BabyTracker.Services;

public interface IMemoriesService
{
    Task SendMemoriesEmail();
}

public class MemoriesService(IConfiguration configuration,
    IAccountService accountService,
    ISqLiteService sqLiteService,
    IEmailRepository emailRepository,
    ILogger<MemoriesService> logger) : IMemoriesService
{
    public async Task SendMemoriesEmail()
    {
        var users = await accountService.SearchUsersWithEnableMemoriesEmail();

        if (users?.Any() != true)
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

                if (memories.Any())
                {
                    var mjml = await GetMJML(memories, userId, baby.Name);
                    await emailRepository.SendEmail(mjml, user, userId, baby.Name);
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
}
