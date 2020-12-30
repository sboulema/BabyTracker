using System;
using System.Threading.Tasks;
using BabyTracker.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;

[DisallowConcurrentExecution]
public class MemoriesJob : IJob
{
    private readonly ILogger<MemoriesJob> _logger;
    private readonly ISqLiteService _sqLiteService;
    private readonly IConfiguration _configuration;

    public MemoriesJob(ILogger<MemoriesJob> logger, ISqLiteService sqLiteService,
        IConfiguration configuration)
    {
        _logger = logger;
        _sqLiteService = sqLiteService;
        _configuration = configuration;
    }

    public Task Execute(IJobExecutionContext context)
    {
        var babyNames = _configuration["MEMORIES_NAMES"].Split(";");

        foreach (var babyName in babyNames)
        {
            var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

            _logger.LogInformation($"Found {memories.Count} memories for {babyName}");
        }

        return Task.CompletedTask;
    }
}