using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using System;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using BabyTracker.Constants;

namespace BabyTracker.Controllers;

[Route("")]
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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // User logged in and has a data clone available
        if (User.Identity?.IsAuthenticated == true &&
            _importService.HasDataClone(User))
        {
            using var db = _sqLiteService.OpenDataConnection(User);

            if (db == null)
            {
                return View(new BaseViewModel());
            }

            var babiesViewModel = new BabiesViewModel
            {
                Babies = await SqLiteService.GetBabiesFromDb(db),
                NickName = User.FindFirstValue("nickname") ?? string.Empty,
                ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
                UserId = User.FindFirstValue("activeUserId") ?? string.Empty,
            };

            var userMetaData = await _accountService.GetUserMetaData(User);
            ViewBag.Theme = userMetaData?.Theme;

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

            var userMetaData = await _accountService.GetUserMetaData(User);
            ViewBag.Theme = userMetaData?.Theme;

            return View("LoggedIn", model);
        }

        // User not logged in
        ViewBag.Theme = ThemesEnum.Auto;

        return View(new BaseViewModel());
    }

    [Authorize]
    [HttpGet("{babyName}/{date?}")]
    public async Task<IActionResult> Diary(string babyName, DateOnly? date)
    {
        using var db = _sqLiteService.OpenDataConnection(User);

        if (db == null)
        {
            return View(new DiaryViewModel());
        }

        var availableDates = await SqLiteService.GetAllEntryDates(babyName, db);

        if (date == null)
        {
            date = availableDates.LastOrDefault();

            if (date == null)
            {
                date = DateOnly.FromDateTime(DateTime.Now);
            }
        }

        var entries = await SqLiteService.GetEntriesFromDb(date.Value, babyName, db);
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

        var memories = await SqLiteService.GetMemoriesFromDb(DateTime.Now, babyName, db);
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        var lastEntryDateTime = await SqLiteService.GetLastEntryDateTime(babyName, db);
        ViewData["LastEntry"] = lastEntryDateTime;

        var userMetaData = await _accountService.GetUserMetaData(User);
        model.FontSize = userMetaData?.FontSize ?? 6;
        ViewBag.Theme = userMetaData?.Theme;

        if ((date == null || date.Value == DateOnly.FromDateTime(lastEntryDateTime)) &&
            userMetaData?.LastViewedDate != null &&
            userMetaData?.LastViewedDate != date)
        {
            TempData["notificationMessage"] = "Do you want to continue where you left off? " + 
                $"<a href=\"{babyName}/{userMetaData!.LastViewedDate!.Value.ToString("yyyy-MM-dd")}\" class=\"alert-link\">{userMetaData!.LastViewedDate!.Value.ToString("yyyy-MM-dd")}</a>";
            TempData["notificationType"] = "info";
        }

        if (date != null && date.Value != DateOnly.FromDateTime(lastEntryDateTime))
        {
            userMetaData!.LastViewedDate = date;
            await _accountService.SaveUserMetaData(User, userMetaData);
        }

        return View("Diary", model);
    }

    [Authorize]
    [HttpGet("{babyName}/memories")]
    public async Task<IActionResult> Memories(string babyName)
    {
        using var db = _sqLiteService.OpenDataConnection(User);

        if (db == null)
        {
            return View(new DiaryViewModel());
        }

        var memories = await SqLiteService.GetMemoriesFromDb(DateTime.Now, babyName, db);
        var model = DiaryService.GetDays(memories);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
        model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
        model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

        var userMetaData = await _accountService.GetUserMetaData(User);
        ViewBag.Theme = userMetaData?.Theme;

        return View("Memories", model);
    }

    [Authorize]
    [HttpGet("{babyName}/charts/{months?}")]
    public async Task<IActionResult> Charts(string babyName, int? months = null)
    {
        using var db = _sqLiteService.OpenDataConnection(User);

        if (db == null)
        {
            return View(new ChartsViewModel());
        }

        var memories = await SqLiteService.GetMemoriesFromDb(DateTime.Now, babyName, db);
        var model = await _chartService.GetViewModel(User, babyName, months + 1);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        model.NickName = User.FindFirstValue("nickname") ?? string.Empty;
        model.ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty;
        model.UserId = User.FindFirstValue("activeUserId") ?? string.Empty;

        var userMetaData = await _accountService.GetUserMetaData(User);
        ViewBag.Theme = userMetaData?.Theme;

        return View("Charts", model);
    }

    [Authorize]
    [HttpGet("{babyName}/gallery")]
    public async Task<IActionResult> Gallery(string babyName)
    {
        using var db = _sqLiteService.OpenDataConnection(User);

        if (db == null)
        {
            return View(new GalleryViewModel());
        }

        var pictures = await SqLiteService.GetPictures(babyName, db);
        var memories = await SqLiteService.GetMemoriesFromDb(DateTime.Now, babyName, db);

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

        var userMetaData = await _accountService.GetUserMetaData(User);
        ViewBag.Theme = userMetaData?.Theme;

        return View("Gallery", model);
    }
}
