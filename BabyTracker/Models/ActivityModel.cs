namespace BabyTracker.Models;

public class ActivityModel : EntryModel
{
    public string OtherActivity { get; set; } = string.Empty;

    public string Duration { get; set; } = string.Empty;

    public string Filename { get; set; } = string.Empty;
}
