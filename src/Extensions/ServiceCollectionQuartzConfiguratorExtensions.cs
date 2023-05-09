using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace BabyTracker.Extensions;

public static class ServiceCollectionQuartzConfiguratorExtensions
{
    public static void AddJobAndTrigger<T>(
        this IServiceCollectionQuartzConfigurator quartz,
        WebApplicationBuilder builder)
        where T : IJob
    {
        if (builder.Environment.IsDevelopment())
        {
            return;
        }

        // Use the name of the IJob as the appsettings.json key
        var jobName = typeof(T).Name;

        // Try and load the schedule from configuration
        var configKey = "MEMORIES_CRON";
        var cronSchedule = builder.Configuration[configKey];

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
