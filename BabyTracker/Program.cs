using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Quartz;
using BabyTracker.Extensions;
using BabyTracker.Jobs;

namespace BabyTracker;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseKestrel(options =>
                {
                    options.Limits.MaxRequestBodySize = null;
                });
            })
            .ConfigureServices((hostContext, services) =>
            {
                services.AddQuartz(q =>
                {
                    q.UseMicrosoftDependencyInjectionJobFactory();

                    q.AddJobAndTrigger<MemoriesJob>(hostContext);
                });

                services.AddQuartzHostedService(
                    q => q.WaitForJobsToComplete = true);
            });
}
