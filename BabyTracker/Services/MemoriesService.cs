using System.Collections.Generic;
using System.Threading.Tasks;
using BabyTracker.Models;
using Microsoft.Extensions.Configuration;
using Mjml.AspNetCore;
using Razor.Templating.Core;

namespace BabyTracker.Services
{
    public interface IMemoriesService 
    {
        Task<string> GetMJML(List<EntryModel> memories, string babyName);
        Task<string> GetHTML(string mjml);
    }

    public class MemoriesService : IMemoriesService
    {
        private readonly IConfiguration _configuration;
        private readonly IMjmlServices _mjmlServices;

        public MemoriesService(IConfiguration configuration, IMjmlServices mjmlServices)
        {
            _configuration = configuration;
            _mjmlServices = mjmlServices;
        }

        public async Task<string> GetMJML(List<EntryModel> memories, string babyName) 
        {
            var importResultModel = new ImportResultModel
            {
                Entries = memories
            };
            
            var model = DiaryService.GetDays(importResultModel);
            
            model.BabyName = babyName;
            model.MemoriesBadgeCount = memories.Count;
            model.ShowMemoriesLink = true;
            model.BaseUrl = _configuration["BASE_URL"];

            var mjml = await RazorTemplateEngine.RenderAsync("/Views/Emails/MemoriesEmail.cshtml", model);

            return mjml;
        }

        public async Task<string> GetHTML(string mjml) 
        {
            var result = await _mjmlServices.Render(mjml);
            return result.Html;
        }
    }
}