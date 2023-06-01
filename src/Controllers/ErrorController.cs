using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using BabyTracker.Models.ViewModels;
using BabyTracker.Constants;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class ErrorController : Controller
{
    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        ViewBag.Theme = ThemesEnum.Auto;

        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
