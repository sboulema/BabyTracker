using System;
using Microsoft.Extensions.Configuration;
using Quartz;

namespace BabyTracker.Extensions;

public static class ServiceCollectionQuartzConfiguratorExtensions
{
    public static void AddJobAndTrigger<T>(
        this IServiceCollectionQuartzConfigurator quartz,
        IConfiguration configuration)
        where T : IJob
    {
        // Use the name of the IJob as the appsettings.json key
        var jobName = typeof(T).Name;

        // Try and load the schedule from configuration
        var configKey = "MEMORIES_CRON";
        var cronSchedule = configuration[configKey];

        // Some minor validation
        if (string.IsNullOrEmpty(cronSchedule))
        {
            throw new Exception($"No Quartz.NET Cron schedule found for job in configuration at {configKey}");
        }

        // register the job as before
        var jobKey = new JobKey(jobName);
        quartz.AddJob<T>(opts => opts.WithIdentity(jobKey));

        quartz.AddTrigger(opts => opts
            .ForJob(jobKey)
            .WithIdentity(jobName + "-trigger")
            .WithCronSchedule(cronSchedule)); // use the schedule from configuration
    }
}
