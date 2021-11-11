using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class ImportController : Controller
{
    [Authorize]
    [HttpGet]
    public IActionResult Import()
    {
        var model = new BaseViewModel
        {
            ShowMemoriesLink = false,
            Profile = AccountService.GetProfile(User)
        };

        return View(model);
    }
}
