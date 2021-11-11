using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class MemoriesController : Controller
{
    private readonly IMemoriesService _memoriesService;

    public MemoriesController(IMemoriesService memoriesService)
    {
        _memoriesService = memoriesService;
    }

    [Authorize]
    [HttpGet("[action]")]
    public async Task Send()
    {
        await _memoriesService.SendMemoriesEmail();
    }
}
