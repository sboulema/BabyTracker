using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using System;
using BabyTracker.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Linq;
using BabyTracker.Constants;
using Microsoft.Extensions.Hosting;
using BabyTracker.Models.Database;
using System.Collections.Generic;

namespace BabyTracker.Controllers;

[Route("")]
public class HomeController : Controller
{
    private readonly IImportService _importService;
    private readonly ISqLiteService _sqLiteService;
    private readonly IChartService _chartService;
    private readonly IAccountService _accountService;
    private readonly IPictureService _pictureService;
    private readonly IHostEnvironment _hostEnvironment;

    public HomeController(
        IImportService importService,
        ISqLiteService sqLiteService,
        IChartService chartService,
        IAccountService accountService,
        IPictureService pictureService,
        IHostEnvironment hostEnvironment)
    {
        _importService = importService;
        _sqLiteService = sqLiteService;
        _chartService = chartService;
        _accountService = accountService;
        _pictureService = pictureService;
        _hostEnvironment = hostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // User logged in and has a data clone available
        if (User.Identity?.IsAuthenticated == true &&
            _importService.HasDataClone(User))
        {
            _sqLiteService.OpenDataConnection(User);

            var babiesViewModel = new BabiesViewModel
            {
                Babies = await _sqLiteService.GetBabiesFromDb(),
                NickName = User.FindFirstValue("nickname") ?? string.Empty,
                ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
                UserId = User.FindFirstValue("activeUserId") ?? string.Empty,
            };

            await _sqLiteService.CloseDataConnection();

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
    public async Task<IActionResult> Diary(string babyName, DateOnly? date, string? q)
    {
        _sqLiteService.OpenDataConnection(User);

        var availableDates = await _sqLiteService.GetAllEntryDates(babyName);

        if (date == null)
        {
            date = availableDates.LastOrDefault();

            if (date == null)
            {
                date = DateOnly.FromDateTime(DateTime.Now);
            }
        }

        var entries = new List<IDbEntry>();

        // Do we need to show entries from a specific day
        // or do we have a search query?
        if (!string.IsNullOrEmpty(q))
        {
            entries = await _sqLiteService.Search(q, babyName);
        }
        else
        {
            entries = await _sqLiteService.GetEntriesFromDb(date.Value, babyName);
        }

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

        var memories = await _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        var lastEntryDateTime = await _sqLiteService.GetLastEntryDateTime(babyName);
        ViewData["LastEntry"] = lastEntryDateTime;

        var userMetaData = await _accountService.GetUserMetaData(User);
        model.FontSize = userMetaData?.FontSize ?? 6;
        ViewBag.Theme = userMetaData?.Theme;

        if ((date == null || date.Value == DateOnly.FromDateTime(lastEntryDateTime)) &&
            userMetaData?.LastViewedDate != null &&
            userMetaData?.LastViewedDate != date)
        {
            TempData["notificationMessage"] = "Do you want to continue where you left off? " + 
                $"<a href=\"{babyName}/{userMetaData!.LastViewedDate!.Value:yyyy-MM-dd}\" class=\"alert-link\">{userMetaData!.LastViewedDate!.Value:yyyy-MM-dd}</a>";
            TempData["notificationType"] = "info";
        }

        if (date != null && date.Value != DateOnly.FromDateTime(lastEntryDateTime))
        {
            userMetaData!.LastViewedDate = date;
            await _accountService.SaveUserMetaData(User, userMetaData);
        }

        await _sqLiteService.CloseDataConnection();

        return View("Diary", model);
    }

    [Authorize]
    [HttpGet("{babyName}/memories")]
    public async Task<IActionResult> Memories(string babyName)
    {
        _sqLiteService.OpenDataConnection(User);

        var memories = await _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

        await _sqLiteService.CloseDataConnection();

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
        _sqLiteService.OpenDataConnection(User);

        var memories = await _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

        await _sqLiteService.CloseDataConnection();

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
        _sqLiteService.OpenDataConnection(User);

        var pictures = await _sqLiteService.GetPictures(babyName);
        var memories = await _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

        await _sqLiteService.CloseDataConnection();

        var errors = _pictureService.CheckPictures(_hostEnvironment, User.FindFirstValue("activeUserId") ?? string.Empty, pictures);

        var model = new GalleryViewModel
        {
            Pictures = pictures,
            BabyName = babyName,
            MemoriesBadgeCount = memories.Count,
            ShowMemoriesLink = true,
            NickName = User.FindFirstValue("nickname") ?? string.Empty,
            ProfileImageUrl = User.FindFirstValue("picture") ?? string.Empty,
            UserId = User.FindFirstValue("activeUserId") ?? string.Empty,
            PicturesMissingCount = errors.Count
        };

        var userMetaData = await _accountService.GetUserMetaData(User);
        ViewBag.Theme = userMetaData?.Theme;

        return View("Gallery", model);
    }
}
