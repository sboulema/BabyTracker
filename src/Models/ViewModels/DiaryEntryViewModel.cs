using System;
using BabyTracker.Constants;
using BabyTracker.Extensions;

namespace BabyTracker.Models.ViewModels;

public class DiaryEntryViewModel
{
    public Guid Id { get; set; }

    /// <summary>The kind of entry. Null for an entry that cannot be deleted.</summary>
    public EntryTypeEnum? Type { get; set; }

    public string IconURL { get; set; } = string.Empty;

    public string Time => TimeStamp.ToDateTimeLocal().ToShortTimeString();

    public string Date => TimeStamp.ToDateTimeLocal().ToString("dd-MM-yyyy");

    public string DateIso => TimeStamp.ToDateTimeLocal().ToString("yyyy-MM-dd");

    public long TimeStamp { get; set; }

    public string CssClass { get; set; } = string.Empty;

    public string CssColor { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public string PictureFileName { get; set; } = string.Empty;

    /// <summary>The address of the page to edit the entry. Empty when the entry cannot be edited.</summary>
    public string EditUrl { get; set; } = string.Empty;
}
