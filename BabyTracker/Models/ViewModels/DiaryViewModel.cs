using System;
using System.Collections.Generic;
using System.Linq;

namespace BabyTracker.Models.ViewModels;

public class DiaryViewModel : BaseViewModel
{
    public IEnumerable<IGrouping<DateTime, EntryModel>> Days { get; set; } = Enumerable.Empty<IGrouping<DateTime, EntryModel>>();

    public IEnumerable<EntryModel> Entries { get; set; } = Enumerable.Empty<EntryModel>();

    public string PictureDirectory { get; set; } = string.Empty;

    public List<string> EntryTypes { get; set; } = new List<string> { "Diaper", "Formula", "Supplement", "Joy", "Growth", "Medication", "Milestone", "Activity", "Sleep", "Temperature", "Vaccine" };

    public string Date { get; set; } = string.Empty;

    public string DateNext { get; set; } = string.Empty;

    public string DatePrevious { get; set; } = string.Empty;
}
