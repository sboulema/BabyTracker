﻿namespace BabyTracker.Models.ViewModels;

public class ProfileViewModel : BaseViewModel
{
    public bool EnableMemoriesEmail { get; set; }

    public string MemoriesAddresses { get; set; } = string.Empty;

    public string ShareList { get; set; } = string.Empty;
}