namespace BabyTracker.Models.ViewModels;

/// <summary>The posted form of the Baby Tracker app sync settings on the profile page.</summary>
public class SyncSettingsViewModel
{
    public bool Enabled { get; set; }

    public string EmailAddress { get; set; } = string.Empty;

    /// <summary>Empty keeps the stored password.</summary>
    public string? Password { get; set; }
}
