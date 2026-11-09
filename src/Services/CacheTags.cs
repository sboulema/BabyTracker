namespace BabyTracker.Services;

public static class CacheTags
{
    /// <summary>Output cache tag shared by all pages that show the clone of this user.</summary>
    public static string Clone(string activeUserId) => $"clone:{activeUserId}";
}
