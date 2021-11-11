using Microsoft.AspNetCore.Mvc;
using BabyTracker.Models;
using BabyTracker.Services;
using System;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace BabyTracker.Controllers;

public class HomeController : Controller
{
    private readonly IImportService _importService;
    private readonly ISqLiteService _sqLiteService;
    private readonly IChartService _chartService;
    private readonly IAccountService _accountService;

    public HomeController(
        IImportService importService,
        ISqLiteService sqLiteService,
        IChartService chartService,
        IAccountService accountService)
    {
        _importService = importService;
        _sqLiteService = sqLiteService;
        _chartService = chartService;
        _accountService = accountService;
    }

    public async Task<IActionResult> Index()
    {
        if (User.Identity?.IsAuthenticated == true &&
            await _importService.HasDataClone())
        {
            var babiesViewModel = new BabiesViewModel
            {
                Profile = await _accountService.GetProfile(User),
                Babies = await _sqLiteService.GetBabiesFromDb(User)
            };

            return View("Babies", babiesViewModel);
        }
        else if (User.Identity?.IsAuthenticated == true)
        {
            var model = new BaseViewModel
            {
                Profile = await _accountService.GetProfile(User)
            };

            return View("LoggedIn", model);
        }

        return View(new BaseViewModel());
    }

    [Authorize]
    [HttpGet("{babyName}/{inputDate?}")]
    public async Task<IActionResult> Diary(string babyName, string inputDate)
    {
        var date = DateTime.Now;

        if (!string.IsNullOrEmpty(inputDate))
        {
            date = DateTime.Parse(inputDate);
        }

        var entries = await _sqLiteService.GetEntriesFromDb(date, User, babyName);
        var importResultModel = new ImportResultModel
        {
            Entries = entries
        };
        var model = DiaryService.GetDays(importResultModel);

        model.Date = date.ToString("yyyy-MM-dd");
        model.DateNext = date.AddDays(1).ToString("yyyy-MM-dd");
        model.DatePrevious = date.AddDays(-1).ToString("yyyy-MM-dd");
        model.BabyName = babyName;
        model.Profile = await _accountService.GetProfile(User);

        var memories = await _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        ViewData["LastEntry"] = await _sqLiteService.GetLastEntryDateTime(User, babyName);

        return View("Diary", model);
    }

    [Authorize]
    [HttpGet("{babyName}/memories")]
    public async Task<IActionResult> Memories(string babyName)
    {
        var memories = await _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);
        var importResultModel = new ImportResultModel
        {
            Entries = memories
        };

        var model = DiaryService.GetDays(importResultModel);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;
        model.Profile = await _accountService.GetProfile(User);

        return View("Memories", model);
    }

    [Authorize]
    [HttpGet("{babyName}/charts/{months?}")]
    public async Task<IActionResult> Charts(string babyName, int? months = null)
    {
        var memories = await _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);
        var model = await _chartService.GetViewModel(User, babyName, months + 1);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;
        model.Profile = await _accountService.GetProfile(User);

        return View("Charts", model);
    }

    [Authorize]
    [HttpGet("{babyName}/gallery")]
    public async Task<IActionResult> Gallery(string babyName)
    {
        var pictures = await _sqLiteService.GetPictures(User, babyName);
        var memories = await _sqLiteService.GetMemoriesFromDb(DateTime.Now, User, babyName);

        var model = new GalleryViewModel
        {
            Pictures = pictures,
            BabyName = babyName,
            MemoriesBadgeCount = memories.Count,
            ShowMemoriesLink = true,
            Profile = await _accountService.GetProfile(User)
        };

        return View("Gallery", model);
    }
}
