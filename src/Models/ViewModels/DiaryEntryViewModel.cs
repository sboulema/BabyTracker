using BabyTracker.Extensions;

namespace BabyTracker.Models.ViewModels;

public class DiaryEntryViewModel
{
    public string IconURL { get; set; } = string.Empty;

    public string Time => TimeStamp.ToDateTimeLocal().ToShortTimeString();

    public string Date => TimeStamp.ToDateTimeLocal().ToString("dd-MM-yyyy");

    public long TimeStamp { get; set; }

    public string CssClass { get; set; } = string.Empty;

    public string CssColor { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public string PictureFileName { get; set; } = string.Empty;
}
