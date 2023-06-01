using System;
using BabyTracker.Constants;

namespace BabyTracker.Models.Account;

public class UserMetaData
{
    public bool EnableMemoriesEmail { get; set; }

    public string MemoriesAddresses { get; set; } = string.Empty;

    public string ShareList { get; set; } = string.Empty;

    public int FontSize { get; set; } = 6;

    public DateOnly? LastViewedDate { get; set; }

    public ThemesEnum Theme { get; set; }
}
