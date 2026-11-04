using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BabyTracker.Services;

public interface IUserDataKeyService
{
    /// <summary>
    /// Returns a copy of the user's key, or null when the user has none. Never creates a key,
    /// so read paths cannot orphan existing encrypted data by minting a replacement.
    /// </summary>
    Task<byte[]?> GetKeyAsync(string userId);

    /// <summary>
    /// Returns a copy of the user's key, creating and persisting one when missing.
    /// Only for import/migration, and only when the clone holds no encrypted files yet.
    /// </summary>
    Task<byte[]> GetOrCreateKeyAsync(string userId);

    /// <summary>Returns a copy of the user's key; throws when the user has none.</summary>
    async Task<byte[]> GetRequiredKeyAsync(string userId)
        => await GetKeyAsync(userId)
           ?? throw new CryptographicException($"No data encryption key exists for user '{userId}'.");
}
