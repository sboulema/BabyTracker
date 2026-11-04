using System.Collections.Generic;

namespace BabyTracker.Services;

/// <summary>
/// Single source of truth for where data clones live on disk, and for the
/// associated data that binds an encrypted file to its location.
/// </summary>
public interface IDataClonePathProvider
{
    string GetRootPath();

    /// <exception cref="System.ArgumentException">The user id is not a safe single path segment.</exception>
    string GetUserPath(string userId);

    string GetDatabasePath(string userId);

    /// <summary>Sibling directory a new import is built in before it replaces the live clone.</summary>
    string GetStagingPath(string userId);

    /// <summary>Sibling directory the previous clone is parked in while an import is swapped in.</summary>
    string GetBackupPath(string userId);

    /// <summary>
    /// Resolves the path of a picture. Returns false when the user id or file name is not a
    /// safe single path segment (e.g. contains separators or "..").
    /// </summary>
    bool TryGetPicturePath(string userId, string fileName, out string path);

    /// <summary>
    /// AES-GCM associated data for a file: ties the ciphertext to its owner and relative path,
    /// so encrypted files cannot be swapped between paths or users without detection.
    /// </summary>
    string GetAssociatedData(string userId, string relativePath);

    /// <summary>User ids that have a data clone directory (excludes staging/backup leftovers).</summary>
    IEnumerable<string> EnumerateUserIds();

    /// <summary>Staging/backup directories left behind by an interrupted import.</summary>
    IEnumerable<string> EnumerateLeftoverDirectories();
}
