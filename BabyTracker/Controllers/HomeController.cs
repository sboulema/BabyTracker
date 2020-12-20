using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Models;
using Microsoft.AspNetCore.Http;
using BabyTracker.Services;
using System;

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
            return View();
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

            _sqLiteService.SetBabyName("zip");
            _sqLiteService.OpenConnection(path);

            return View("Diary", null);
        }

        [HttpPost]
        public IActionResult LoadFile(string babyName)
        {
            var path = _importService.HandleLoad(babyName);

            if (string.IsNullOrEmpty(path)) 
            {
                return View("Error", new ErrorViewModel { Message = $"Unable to load find data for baby '{babyName}'" });
            }

            _sqLiteService.SetBabyName(babyName);
            _sqLiteService.OpenConnection(path);

            return View("Diary", null);
        }

        [HttpGet("{inputDate}")]
        public IActionResult Diary(string inputDate)
        {
            var date = DateTime.Parse(inputDate);

            var entries = _sqLiteService.GetEntriesFromDb(date);
            var importResultModel = new ImportResultModel
            {
                Entries = entries
            };
            var model = DiaryService.GetDays(importResultModel);

            model.Date = date.ToString("yyyy-MM-dd");
            model.DateNext = date.AddDays(1).ToString("yyyy-MM-dd");
            model.DatePrevious = date.AddDays(-1).ToString("yyyy-MM-dd");

            return View("Diary", model);
        }

        [HttpGet("memories")]
        public IActionResult Memories()
        {
            var entries = _sqLiteService.GetMemoriesFromDb(DateTime.Now);
            var importResultModel = new ImportResultModel
            {
                Entries = entries
            };
            var model = DiaryService.GetDays(importResultModel);
            
            return View("Memories", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
