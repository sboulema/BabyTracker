using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Models;
using Microsoft.AspNetCore.Http;
using BabyTracker.Services;
using System;
using BabyTracker.Models.ViewModels;
using System.IO;
using System.Threading.Tasks;

namespace BabyTracker.Controllers;

public class HomeController : Controller
{
    private readonly IImportService _importService;
    private readonly ISqLiteService _sqLiteService;
    private readonly IMemoriesService _memoriesService;
    private readonly IChartService _chartService;

    public HomeController(
        IImportService importService,
        ISqLiteService sqLiteService,
        IMemoriesService memoriesService,
        IChartService chartService)
    {
        _importService = importService;
        _sqLiteService = sqLiteService;
        _memoriesService = memoriesService;
        _chartService = chartService;
    }

    public IActionResult Index()
    {
        var model = new BaseViewModel
        {
            ShowMemoriesLink = false
        };

        return View(model);
    }

    [HttpPost]
    [DisableRequestSizeLimit]
    public IActionResult ImportFile(IFormFile file)
    {
        var path = _importService.HandleImport(file);

        if (string.IsNullOrEmpty(path))
        {
            return View("Error", new ErrorViewModel { Message = "Unable to import file" });
        }

        return Redirect($"/{Path.GetFileNameWithoutExtension(file.FileName)}/{DateTime.Now.ToString("yyyy-MM-dd")}");
    }

    [HttpPost]
    public IActionResult LoadFile(string babyName)
    {
        var path = _importService.HandleLoad(babyName);

        if (string.IsNullOrEmpty(path))
        {
            return View("Error", new ErrorViewModel { Message = $"Unable to load find data for baby '{babyName}'" });
        }

        return Redirect($"/{babyName}/{DateTime.Now.ToString("yyyy-MM-dd")}");
    }

    [HttpGet("{babyName}/{inputDate?}")]
    public IActionResult Diary(string babyName, string inputDate)
    {
        var date = DateTime.Now;

        if (!string.IsNullOrEmpty(inputDate))
        {
            date = DateTime.Parse(inputDate);
        }

        var entries = _sqLiteService.GetEntriesFromDb(date, babyName);
        var importResultModel = new ImportResultModel
        {
            Entries = entries
        };
        var model = DiaryService.GetDays(importResultModel);

        model.Date = date.ToString("yyyy-MM-dd");
        model.DateNext = date.AddDays(1).ToString("yyyy-MM-dd");
        model.DatePrevious = date.AddDays(-1).ToString("yyyy-MM-dd");
        model.BabyName = babyName;

        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        ViewData["LastEntry"] = _sqLiteService.GetLastEntryDateTime(babyName);

        return View("Diary", model);
    }

    [HttpGet("{babyName}/memories")]
    public IActionResult Memories(string babyName)
    {
        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);
        var importResultModel = new ImportResultModel
        {
            Entries = memories
        };

        var model = DiaryService.GetDays(importResultModel);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        return View("Memories", model);
    }

    [HttpGet("{babyName}/memories/email")]
    public async Task<IActionResult> MemoriesEmail(string babyName)
    {
        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

        var mjml = await _memoriesService.GetMJML(memories, babyName);
        var html = await _memoriesService.GetHTML(mjml);

        return Content(html, "text/html");
    }

    [HttpGet("{babyName}/charts/{months?}")]
    public IActionResult Charts(string babyName, int? months = null)
    {
        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);
        var model = _chartService.GetViewModel(babyName, months + 1);

        model.BabyName = babyName;
        model.MemoriesBadgeCount = memories.Count;
        model.ShowMemoriesLink = true;

        return View("Charts", model);
    }

    [HttpGet("{babyName}/gallery")]
    public IActionResult Gallery(string babyName)
    {
        var pictures = _sqLiteService.GetPictures(babyName);
        var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);

        var model = new GalleryViewModel
        {
            Pictures = pictures,
            BabyName = babyName,
            MemoriesBadgeCount = memories.Count,
            ShowMemoriesLink = true
        };

        return View("Gallery", model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
