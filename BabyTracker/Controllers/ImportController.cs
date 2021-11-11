using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
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
            Profile = await _accountService.GetProfile(User)
        };

        return View(model);
    }
}
