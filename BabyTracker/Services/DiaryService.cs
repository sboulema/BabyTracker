using BabyTracker.Models;
using BabyTracker.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace BabyTracker.Services
{
    public static class DiaryService
    {
        public static DiaryViewModel GetDays(List<EntryModel> entries)
        {
            return new DiaryViewModel
            {
                Days = entries.OrderByDescending(e => e.Time).GroupBy(e => e.Time.Date)
            };
        }
    }
}
