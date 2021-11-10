using Microsoft.AspNetCore.Mvc;
using BabyTracker.Models;
using BabyTracker.Services;
using System;
using BabyTracker.Models.ViewModels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BabyTracker.Controllers;

public class HomeController : Controller
{
    private readonly ISqLiteService _sqLiteService;
    private readonly IMemoriesService _memoriesService;
    private readonly IChartService _chartService;

    public HomeController(
        ISqLiteService sqLiteService,
        IMemoriesService memoriesService,
        IChartService chartService)
    {
        _sqLiteService = sqLiteService;
        _memoriesService = memoriesService;
        _chartService = chartService;
    }

    public IActionResult Index()
    {
        var model = new BaseViewModel
        {
            ShowMemoriesLink = false,
            Profile = AccountService.GetProfile(User)
        };

        if (User.Identity?.IsAuthenticated == true &&
            ImportService.HasDataClone(User))
        {
            var babiesViewModel = new BabiesViewModel
            {
                Profile = AccountService.GetProfile(User),
                Babies = _sqLiteService.GetBabiesFromDb(User)
            };

            return View("Babies", babiesViewModel);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            return View("LoggedIn", model);
        }

        return View(model);
    }

    [Authorize]
    [HttpGet("{babyName}/{inputDate?}")]
    public IActionResult Diary(string babyName, string inputDate)
    {
        var date = DateTime.Now;

        if (!string.IsNullOrEmpty(inputDate))
        {
            date = DateTime.Parse(inputDate);
        }

        var entries = _sqLiteService.GetEntriesFromDb(date, User, babyName);
        var importResultModel = new ImportResultModel
        {
            Entries = entries
        };
        var model = DiaryService.GetDays(importResultModel);

        model.Date = date.ToString("yyyy-MM-dd");
        model.DateNext = date.AddDays(1).ToString("yyyy-MM-dd");
        model.DatePrevious = date.AddDays(-1).ToString("yyyy-MM-dd");
        model.BabyName = babyName;
        model.Profile = AccountService.GetProfile(User);

        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        ViewData["LastEntry"] = _sqLiteService.GetLastEntryDateTime(User, babyName);

        return View("Diary", model);
    }

    [Authorize]
    [HttpGet("{babyName}/memories")]
    public IActionResult Memories(string babyName)
    {
        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);
        var importResultModel = new ImportResultModel
        {
            Entries = memories
        };

        var model = DiaryService.GetDays(importResultModel);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;
        model.Profile = AccountService.GetProfile(User);

        return View("Memories", model);
    }

    [HttpGet("{babyName}/memories/email")]
    public async Task<IActionResult> MemoriesEmail(string babyName)
    {
        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);

        var mjml = await _memoriesService.GetMJML(memories, babyName);
        var html = await _memoriesService.GetHTML(mjml);

        return Content(html, "text/html");
    }

    [Authorize]
    [HttpGet("{babyName}/charts/{months?}")]
    public IActionResult Charts(string babyName, int? months = null)
    {
        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);
        var model = _chartService.GetViewModel(User, babyName, months + 1);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;
        model.Profile = AccountService.GetProfile(User);

        return View("Charts", model);
    }

    [Authorize]
    [HttpGet("{babyName}/gallery")]
    public IActionResult Gallery(string babyName)
    {
        var pictures = _sqLiteService.GetPictures(User, babyName);
        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);

        var model = new GalleryViewModel
        {
            Pictures = pictures,
            BabyName = babyName,
            MemoriesBadgeCount = memories.Count,
            ShowMemoriesLink = true,
            Profile = AccountService.GetProfile(User)
        };

        return View("Gallery", model);
    }
}
