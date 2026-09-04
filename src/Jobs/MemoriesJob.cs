using System.Threading;
using System.Threading.Tasks;
using BabyTracker.Services;
using Quartz;

namespace BabyTracker.Jobs;

[DisallowConcurrentExecution]
public class MemoriesJob(IMemoriesService memoriesService) : IJob
{
    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        => await memoriesService.SendMemoriesEmail();
}
