using System;
using System.Collections.Generic;
using System.Linq;
using BabyTracker.Models.Database;

namespace BabyTracker.Models.ViewModels;

public class DiaryViewModel : BaseViewModel
{
    public IEnumerable<IGrouping<DateTime, IDbEntry>> Days { get; set; } = Enumerable.Empty<IGrouping<DateTime, IDbEntry>>();

    public IEnumerable<IDbEntry> Entries { get; set; } = Enumerable.Empty<IDbEntry>();

    public string PictureDirectory { get; set; } = string.Empty;

    public List<string> EntryTypes { get; set; } = new List<string> { "Diaper", "Formula", "Supplement", "Joy", "Growth", "Medication", "Milestone", "Activity", "Sleep", "Temperature", "Vaccine" };

    public DateOnly Date { get; set; }

    public List<DateOnly> AvailableDates = new();

    public string DateNextUrl { get; set; } = string.Empty;

    public string DatePreviousUrl { get; set; } = string.Empty;

    public int FontSize { get; set; } = 6;
}
