using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using BabyTracker.Services;

namespace BabyTracker.Sync;

public interface ISyncAccountStore
{
    /// <returns>null when the user has no sync account.</returns>
    Task<SyncAccount?> GetAsync(string userId);

    Task SaveAsync(string userId, SyncAccount account);

    Task DeleteAsync(string userId);
}

/// <summary>
/// Keeps the sync account, which includes the password for the Baby Tracker server, encrypted with
/// the data key of the user, the same way as the data clone.
/// </summary>
public sealed class SyncAccountStore(
    IDataClonePathProvider pathProvider,
    IUserDataKeyService userDataKeyService,
    IDataEncryptionService dataEncryptionService) : ISyncAccountStore
{
    private const string AssociatedDataName = "sync-account.json";

    public async Task<SyncAccount?> GetAsync(string userId)
    {
        var path = pathProvider.GetSyncAccountPath(userId);

        if (!File.Exists(path))
        {
            return null;
        }

        var key = await userDataKeyService.GetKeyAsync(userId);

        if (key is null)
        {
            return null;
        }

        try
        {
            var plaintext = dataEncryptionService.DecryptFile(
                await File.ReadAllBytesAsync(path),
                key,
                pathProvider.GetAssociatedData(userId, AssociatedDataName));

            try
            {
                return JsonSerializer.Deserialize<SyncAccount>(plaintext);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
        }
    }

    public async Task SaveAsync(string userId, SyncAccount account)
    {
        var path = pathProvider.GetSyncAccountPath(userId);
        var key = await userDataKeyService.GetOrCreateKeyAsync(userId);
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(account);

        try
        {
            using var stream = new MemoryStream(plaintext);

            // Encrypts and replaces the file atomically
            dataEncryptionService.EncryptStream(
                stream,
                path,
                key,
                pathProvider.GetAssociatedData(userId, AssociatedDataName));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public Task DeleteAsync(string userId)
    {
        var path = pathProvider.GetSyncAccountPath(userId);

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }
}
