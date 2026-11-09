namespace BabyTracker.Sync;

public sealed class BabyTrackerSyncOptions
{
    public const string SectionName = "BabyTrackerSync";

    /// <summary>
    /// Master switch. While this is false, the sync settings are hidden and nothing is ever sent
    /// to the Baby Tracker server.
    /// </summary>
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "https://prodapp.babytrackers.com";
}
