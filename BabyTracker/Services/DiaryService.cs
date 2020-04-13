using BabyTracker.Models;
using BabyTracker.Models.ViewModels;
using System.Linq;

namespace BabyTracker.Services
{
    public static class DiaryService
    {
        public static DiaryViewModel GetDays(ImportResultModel model)
            => new DiaryViewModel
            {
                Days = model.Entries.OrderByDescending(e => e.Time).GroupBy(e => e.Time.Date)
            };
    }
}
