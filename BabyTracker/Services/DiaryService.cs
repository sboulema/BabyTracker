using BabyTracker.Extensions;
using BabyTracker.Models.Database;
using BabyTracker.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace BabyTracker.Services;

public static class DiaryService
{
    public static DiaryViewModel GetDays(List<IDbEntry> entries)
        => new()
        {
            Days = entries.OrderByDescending(entry => entry.Time).GroupBy(e => e.Time.ToDateTimeUTC().Date),
            Entries = entries.OrderByDescending(e => e.Time)
        };
}
