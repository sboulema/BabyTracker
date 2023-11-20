using Microsoft.AspNetCore.Mvc;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BabyTracker.Services;
using System.Threading.Tasks;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class ImportController(IAccountService accountService) : Controller
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

        var userMetaData = await accountService.GetUserMetaData(User);
        ViewBag.Theme = userMetaData?.Theme;

        return View(model);
    }
}
