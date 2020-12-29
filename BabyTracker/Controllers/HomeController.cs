using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Models;
using Microsoft.AspNetCore.Http;
using BabyTracker.Services;
using System;
using BabyTracker.Models.ViewModels;
using System.IO;

namespace BabyTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly IImportService _importService;
        private readonly ISqLiteService _sqLiteService;

        public HomeController(
            IImportService importService,
            ISqLiteService sqLiteService)
        {
            _importService = importService;
            _sqLiteService = sqLiteService;
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

            _sqLiteService.OpenConnection(path);

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

            _sqLiteService.OpenConnection(path);

            return Redirect($"/{babyName}/{DateTime.Now.ToString("yyyy-MM-dd")}");
        }

        [HttpGet("{babyName}/{inputDate}")]
        public IActionResult Diary(string babyName, string inputDate)
        {
            var date = DateTime.Parse(inputDate);

            var entries = _sqLiteService.GetEntriesFromDb(date, babyName);
            var importResultModel = new ImportResultModel
            {
                Entries = entries
            };
            var model = DiaryService.GetDays(importResultModel);

            model.Date = date.ToString("yyyy-MM-dd");
            model.DateNext = date.AddDays(1).ToString("yyyy-MM-dd");
            model.DatePrevious = date.AddDays(-1).ToString("yyyy-MM-dd");

            var memories = _sqLiteService.GetMemoriesFromDb(DateTime.Now, babyName);
            model.MemoriesBadgeCount = memories.Count;
            model.ShowMemoriesLink = true;

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
            
            model.MemoriesBadgeCount = memories.Count;
            model.ShowMemoriesLink = true;

            return View("Memories", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
