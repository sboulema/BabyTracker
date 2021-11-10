﻿namespace BabyTracker.Models.Account;

public class UserMetaData
{
    public bool EnableMemoriesEmail { get; set; }

    public string MemoriesAddresses { get; set; } = string.Empty;

    public string ShareList { get; set; } = string.Empty;
}
