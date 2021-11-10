using BabyTracker.Models.Account;

namespace BabyTracker.Models.ViewModels;

public class BaseViewModel
{
    public bool ShowMemoriesLink { get; set; }

    public int MemoriesBadgeCount { get; set; }

    public string BabyName { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public Profile? Profile { get; set; }
}
