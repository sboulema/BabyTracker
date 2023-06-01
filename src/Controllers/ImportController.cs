using Microsoft.AspNetCore.Mvc;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using BabyTracker.Services;
using System.Threading.Tasks;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class ImportController : Controller
{
    private readonly IAccountService _accountService;

    public ImportController(IAccountService accountService)
    {
        _accountService = accountService;
    }

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

        var userMetaData = await _accountService.GetUserMetaData(User);
        ViewBag.Theme = userMetaData?.Theme;

        return View(model);
    }
}
