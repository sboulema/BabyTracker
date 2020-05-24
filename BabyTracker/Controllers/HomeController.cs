using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BabyTracker.Models;
using Microsoft.AspNetCore.Http;
using BabyTracker.Services;

namespace BabyTracker.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IImportService _importService;

        public HomeController(ILogger<HomeController> logger, IImportService importService)
        {
            _logger = logger;
            _importService = importService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [DisableRequestSizeLimit]
        public IActionResult ImportFile(IFormFile file)
        {
            var importResultModel = _importService.HandleImport(file);

            var model = DiaryService.GetDays(importResultModel);

            return View("Diary", model);
        }

        [HttpPost]
        public IActionResult LoadFile(string fileName)
        {
            var importResultModel = _importService.LoadFromZip(fileName);

            if (importResultModel == null) 
            {
                return View("Error", new ErrorViewModel { Message = "Unable to load file" });
            }

            var model = DiaryService.GetDays(importResultModel);

            return View("Diary", model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
