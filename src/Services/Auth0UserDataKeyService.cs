using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Auth0.ManagementApi;

namespace BabyTracker.Services;

/// <remarks>
/// Assumes a single application instance: the per-user lock and the key cache are in-process,
/// and Auth0 has no conditional update, so two instances creating a key at the same time could
/// overwrite each other.
/// </remarks>
public sealed class Auth0UserDataKeyService(IManagementApiClient managementApiClient) : IUserDataKeyService
{
    private const string MetadataKey = "BabyTrackerDataEncryptionKey";
    private const int KeySize = 32;

    private readonly static ConcurrentDictionary<string, byte[]> KeyCache = new();
    private readonly static ConcurrentDictionary<string, SemaphoreSlim> UserLocks = new();

    public Task<byte[]?> GetKeyAsync(string userId)
        => ResolveKeyAsync(userId, createIfMissing: false);

    public async Task<byte[]> GetOrCreateKeyAsync(string userId)
        => await ResolveKeyAsync(userId, createIfMissing: true)
           ?? throw new CryptographicException($"Could not create a data encryption key for user '{userId}'.");

    private async Task<byte[]?> ResolveKeyAsync(string userId, bool createIfMissing)
    {
        var auth0UserId = userId.StartsWith("auth0|", StringComparison.Ordinal)
            ? userId
            : $"auth0|{userId}";

        if (KeyCache.TryGetValue(auth0UserId, out var cachedKey))
        {
            return [.. cachedKey];
        }

        var userLock = UserLocks.GetOrAdd(auth0UserId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync();

        try
        {
            if (KeyCache.TryGetValue(auth0UserId, out cachedKey))
            {
                return [.. cachedKey];
            }

            var user = await managementApiClient.Users.GetAsync(auth0UserId, new());

            if (user.AppMetadata is not null &&
                user.AppMetadata.TryGetValue(MetadataKey, out var value))
            {
                // Never replace a stored key: that would make existing encrypted files unreadable.
                if (!TryReadKey(value, out var existingKey))
                {
                    throw new CryptographicException($"The stored encryption key for Auth0 user '{auth0UserId}' is invalid.");
                }

                KeyCache[auth0UserId] = existingKey;
                return [.. existingKey];
            }

            if (!createIfMissing)
            {
                return null;
            }

            var key = RandomNumberGenerator.GetBytes(KeySize);

            // app_metadata is merged per top-level property, so only the new key is sent;
            // other app_metadata values are left untouched.
            await managementApiClient.Users.UpdateAsync(auth0UserId, new()
            {
                AppMetadata = new Dictionary<string, object?>
                {
                    [MetadataKey] = Convert.ToBase64String(key)
                }
            });

            KeyCache[auth0UserId] = key;
            return [.. key];
        }
        finally
        {
            userLock.Release();
        }
    }

    /// <summary>
    /// The Management API client deserializes app_metadata values into <c>object</c>, which
    /// System.Text.Json materializes as <see cref="JsonElement"/>, not <see cref="string"/>.
    /// </summary>
    private static bool TryReadKey(object? value, out byte[] key)
    {
        key = [];

        var encodedKey = value switch
        {
            string text => text,
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString(),
            _ => null
        };

        if (encodedKey is null)
        {
            return false;
        }

        try
        {
            var decoded = Convert.FromBase64String(encodedKey);

            if (decoded.Length != KeySize)
            {
                return false;
            }

            key = decoded;
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
