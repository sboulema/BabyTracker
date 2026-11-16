using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public sealed class DataClonePathProvider(IWebHostEnvironment webHostEnvironment) : IDataClonePathProvider
{
    public const string DatabaseFileName = "EasyLog.db";

    private const string StagingMarker = ".importing-";
    private const string BackupMarker = ".previous-";

    public string GetRootPath()
        => webHostEnvironment.IsProduction()
            ? "/data/Data"
            : Path.Combine(webHostEnvironment.ContentRootPath, "Data");

    public string GetUserPath(string userId)
    {
        if (!IsSafeSegment(userId))
        {
            throw new ArgumentException("The user id is not a valid directory name.", nameof(userId));
        }

        return Path.Combine(GetRootPath(), userId);
    }

    public string GetDatabasePath(string userId)
        => Path.Combine(GetUserPath(userId), DatabaseFileName);

    public string GetStagingPath(string userId)
        => $"{GetUserPath(userId)}{StagingMarker}{Guid.NewGuid():N}";

    public string GetBackupPath(string userId)
        => $"{GetUserPath(userId)}{BackupMarker}{Guid.NewGuid():N}";

    public bool TryGetPicturePath(string userId, string fileName, out string path)
    {
        if (!IsSafeSegment(userId) || !IsSafeSegment(fileName))
        {
            path = string.Empty;
            return false;
        }

        path = Path.Combine(GetUserPath(userId), $"{fileName}.jpg");
        return true;
    }

    public string GetSyncAccountPath(string userId)
    {
        if (!IsSafeSegment(userId))
        {
            throw new ArgumentException("The user id is not a valid file name.", nameof(userId));
        }

        var root = GetRootPath();
        var parent = Path.GetDirectoryName(root.TrimEnd('/', '\\')) ?? root;

        return Path.Combine(parent, "Sync", $"{userId}.json.enc");
    }

    public string GetAssociatedData(string userId, string relativePath)
        => $"{userId}/{relativePath.Replace('\\', '/').TrimStart('/')}";

    public IEnumerable<string> EnumerateUserIds()
        => EnumerateRootDirectoryNames().Where(name => !IsTemporaryDirectoryName(name));

    public IEnumerable<string> EnumerateLeftoverDirectories()
        => EnumerateRootDirectoryNames()
            .Where(IsTemporaryDirectoryName)
            .Select(name => Path.Combine(GetRootPath(), name));

    private IEnumerable<string> EnumerateRootDirectoryNames()
    {
        var root = GetRootPath();

        if (!Directory.Exists(root))
        {
            return Enumerable.Empty<string>();
        }

        return Directory.EnumerateDirectories(root)
            .Select(directory => Path.GetFileName(directory))
            .OfType<string>();
    }

    private static bool IsTemporaryDirectoryName(string name)
        => name.Contains(StagingMarker, StringComparison.Ordinal) ||
           name.Contains(BackupMarker, StringComparison.Ordinal);

    private static bool IsSafeSegment(string? value)
        => !string.IsNullOrWhiteSpace(value) &&
           value != "." &&
           value != ".." &&
           value.IndexOfAny(['/', '\\', '\0']) < 0 &&
           value.IndexOfAny(Path.GetInvalidFileNameChars()) < 0;
}
