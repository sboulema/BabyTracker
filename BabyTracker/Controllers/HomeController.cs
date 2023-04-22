using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using System;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;

namespace BabyTracker.Controllers;

[Route("")]
public class HomeController : Controller
{
    private readonly IImportService _importService;
    private readonly ISqLiteService _sqLiteService;
    private readonly IChartService _chartService;

    public HomeController(
        IImportService importService,
        ISqLiteService sqLiteService,
        IChartService chartService)
    {
        _importService = importService;
        _sqLiteService = sqLiteService;
        _chartService = chartService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // User logged in and has a data clone available
        if (User.Identity?.IsAuthenticated == true &&
            await _importService.HasDataClone())
        {
            var babiesViewModel = new BabiesViewModel
            {
                Babies = await _sqLiteService.GetBabiesFromDb(User),
                NickName = User.FindFirstValue("nickname") ?? string.Empty,
                ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
                UserId = User.FindFirstValue("activeUserId") ?? string.Empty,
            };

            return View("Babies", babiesViewModel);
        }
        
        // User logged in but no data clone available
        if (User.Identity?.IsAuthenticated == true)
        {
            var model = new BaseViewModel
            {
                NickName = User.FindFirstValue("nickname") ?? string.Empty,
                ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
                UserId = User.FindFirstValue("activeUserId") ?? string.Empty
            };

            return View("LoggedIn", model);
        }

        // User not logged in
        return View(new BaseViewModel());
    }

    [Authorize]
    [HttpGet("{babyName}/{date?}")]
    public async Task<IActionResult> Diary(string babyName, DateOnly? date)
    {
        var availableDates = await _sqLiteService.GetAllEntryDates(User, babyName);

        if (date == null)
        {
            date = availableDates.LastOrDefault();

            if (date == null)
            {
                date = DateOnly.FromDateTime(DateTime.Now);
            }
        }

        var entries = await _sqLiteService.GetEntriesFromDb(date.Value, User, babyName);
        var model = DiaryService.GetDays(entries);

        var dateNext = availableDates.SkipWhile(availableDate => availableDate != date).Skip(1).FirstOrDefault();
        var datePrevious = availableDates.TakeWhile(availableDate => availableDate != date).LastOrDefault();

        model.AvailableDates = availableDates;
        model.Date = date.Value;
        model.DateNextUrl = dateNext != DateOnly.MinValue ? $"/{babyName}/{dateNext:yyyy-MM-dd}" : string.Empty;
        model.DatePreviousUrl = datePrevious != DateOnly.MinValue ? $"/{babyName}/{datePrevious:yyyy-MM-dd}" : string.Empty;
        model.BabyName = babyName;

        model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
        model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
        model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

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
        var model = DiaryService.GetDays(memories);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
        model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
        model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

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

        model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
        model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
        model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

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
            NickName = User.FindFirstValue("nickname") ?? string.Empty,
            ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
            UserId = User.FindFirstValue("activeUserId") ?? string.Empty
        };

        return View("Gallery", model);
    }
}
