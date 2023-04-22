using Microsoft.AspNetCore.Mvc;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class ImportController : Controller
{
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Import()
    {
        var model = new BaseViewModel
        {
            ShowMemoriesLink = false,
            NickName = User.FindFirstValue("nickname") ?? string.Empty,
            ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
            UserId = User.FindFirstValue("userId") ?? string.Empty,
        };

        return View(model);
    }
}
