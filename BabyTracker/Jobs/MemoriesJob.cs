using System.Threading.Tasks;
using BabyTracker.Services;
using Quartz;

namespace BabyTracker.Jobs;

[DisallowConcurrentExecution]
public class MemoriesJob : IJob
{
    private readonly IMemoriesService _memoriesService;

    public MemoriesJob(IMemoriesService memoriesService)
    {
        _memoriesService = memoriesService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        await _memoriesService.SendMemoriesEmail();
    }
}
