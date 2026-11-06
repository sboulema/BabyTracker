using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BabyTracker.Services;

public interface IImportService
{
    Task<string> Unzip(ClaimsPrincipal user, Stream stream);

    bool HasDataClone(ClaimsPrincipal user);

    Task EnsureDataCloneEncrypted(string userId);
}

public class ImportService(ICacheEvictionService cacheEvictionService,
    IUserDataKeyService userDataKeyService,
    IDataEncryptionService dataEncryptionService,
    IDataClonePathProvider pathProvider,
    ILogger<ImportService> logger) : IImportService
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> UserLocks = new();

    /// <summary>
    /// Extracts the archive straight into encrypted files inside a staging directory
    /// (plaintext never touches the disk), then swaps the staging directory in place of
    /// the existing clone. A failed import leaves the previous clone untouched.
    /// </summary>
    public async Task<string> Unzip(ClaimsPrincipal user, Stream stream)
    {
        var activeUserId = user.FindFirstValue("activeUserId");
        if (string.IsNullOrWhiteSpace(activeUserId))
        {
            throw new InvalidOperationException("No active user is available for this import.");
        }

        var targetPath = pathProvider.GetUserPath(activeUserId);
        var stagingPath = pathProvider.GetStagingPath(activeUserId);

        using var userLock = await AcquireLockAsync(activeUserId);

        // Safe to create a key here: an import replaces the whole clone, so there are no
        // existing encrypted files that a new key could orphan.
        var key = await userDataKeyService.GetOrCreateKeyAsync(activeUserId);

        try
        {
            Directory.CreateDirectory(stagingPath);

            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
            {
                archive.Entries
                    .Where(entry => !string.IsNullOrEmpty(entry.Name))
                    .ToList()
                    .ForEach(entry => EncryptEntry(entry, stagingPath, activeUserId, key));
            }

            SwapIntoPlace(activeUserId, stagingPath, targetPath);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Importing the data clone for user {UserId} failed", activeUserId);

            DeleteDirectoryQuietly(stagingPath);

            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }

        await cacheEvictionService.EvictUserPagesAsync(user);

        return targetPath;
    }

    /// <summary>
    /// Encrypts any plaintext files left in the user's clone (clones imported before encryption
    /// existed). Cheap when nothing is left to do: only file headers are read.
    /// </summary>
    public async Task EnsureDataCloneEncrypted(string userId)
    {
        var userPath = pathProvider.GetUserPath(userId);
        if (!Directory.Exists(userPath))
        {
            return;
        }

        using var userLock = await AcquireLockAsync(userId);

        var allFiles = Directory.EnumerateFiles(userPath, "*", SearchOption.AllDirectories).ToArray();
        var plaintextFiles = allFiles
            .Where(file => !dataEncryptionService.IsEncrypted(file))
            .ToArray();

        if (plaintextFiles.Length == 0)
        {
            return;
        }

        // A clone that already holds encrypted files must keep using its existing key.
        // Minting a new one would make those files permanently unreadable.
        var hasEncryptedFiles = plaintextFiles.Length < allFiles.Length;
        var key = hasEncryptedFiles
            ? await userDataKeyService.GetRequiredKeyAsync(userId)
            : await userDataKeyService.GetOrCreateKeyAsync(userId);

        try
        {
            plaintextFiles
                .ToList()
                .ForEach(file => dataEncryptionService.EncryptFile(
                    file,
                    key,
                    pathProvider.GetAssociatedData(userId, Path.GetRelativePath(userPath, file))));

            logger.LogInformation(
                "Encrypted {Count} plaintext file(s) in the data clone of user {UserId}",
                plaintextFiles.Length,
                userId);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public bool HasDataClone(ClaimsPrincipal user)
    {
        var activeUserId = user.FindFirstValue("activeUserId");
        if (string.IsNullOrWhiteSpace(activeUserId))
        {
            return false;
        }

        return File.Exists(pathProvider.GetDatabasePath(activeUserId));
    }

    private void EncryptEntry(ZipArchiveEntry entry, string stagingPath, string userId, byte[] key)
    {
        var stagingRoot = Path.GetFullPath(stagingPath);
        var destinationPath = Path.GetFullPath(Path.Combine(stagingRoot, entry.FullName));

        // The archive is user-supplied: refuse entries that would land outside the staging directory.
        if (!destinationPath.StartsWith(stagingRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            throw new IOException($"The archive entry '{entry.FullName}' is outside the target directory.");
        }

        using var entryStream = entry.Open();

        dataEncryptionService.EncryptStream(
            entryStream,
            destinationPath,
            key,
            pathProvider.GetAssociatedData(userId, Path.GetRelativePath(stagingRoot, destinationPath)));
    }

    private void SwapIntoPlace(string userId, string stagingPath, string targetPath)
    {
        var hadPreviousClone = Directory.Exists(targetPath);
        var backupPath = pathProvider.GetBackupPath(userId);

        if (hadPreviousClone)
        {
            Directory.Move(targetPath, backupPath);
        }

        try
        {
            Directory.Move(stagingPath, targetPath);
        }
        catch
        {
            if (hadPreviousClone)
            {
                Directory.Move(backupPath, targetPath);
            }

            throw;
        }

        if (hadPreviousClone)
        {
            DeleteDirectoryQuietly(backupPath);
        }
    }

    private void DeleteDirectoryQuietly(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Could not remove directory '{Path}'", path);
        }
    }

    private static async Task<IDisposable> AcquireLockAsync(string userId)
    {
        var semaphore = UserLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync();

        return new LockReleaser(semaphore);
    }

    private sealed class LockReleaser(SemaphoreSlim semaphore) : IDisposable
    {
        public void Dispose() => semaphore.Release();
    }
}
