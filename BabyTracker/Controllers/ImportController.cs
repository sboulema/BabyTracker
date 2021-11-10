using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
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

    [Authorize]
    [HttpPost]
    [DisableRequestSizeLimit]
    public IActionResult ImportFile(IFormFile file)
    {
        var path = ImportService.HandleImport(file, User);

        if (string.IsNullOrEmpty(path))
        {
            return View("Error", new ErrorViewModel { Message = "Unable to import file" });
        }

        return RedirectToAction("Index", "Home");
    }
}
