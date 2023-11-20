using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class MemoriesController(IMemoriesService memoriesService) : Controller
{
    /// <summary>
    /// Used for testing the memories job
    /// </summary>
    /// <returns></returns>
    [Authorize]
    [HttpGet("[action]")]
    public async Task Send() => await memoriesService.SendMemoriesEmail();
}
