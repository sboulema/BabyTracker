namespace BabyTracker.Models.ViewModels;

public class BaseViewModel
{
    public bool ShowMemoriesLink { get; set; }

    public int MemoriesBadgeCount { get; set; }

    public string BabyName { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public string ProfileImageUrl { get; set; } = string.Empty;

    public string NickName { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;
}
