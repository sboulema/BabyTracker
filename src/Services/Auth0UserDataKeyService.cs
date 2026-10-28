using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Auth0.ManagementApi;

namespace BabyTracker.Services;

public sealed class Auth0UserDataKeyService(IManagementApiClient managementApiClient) : IUserDataKeyService
{
    private const string MetadataKey = "BabyTrackerDataEncryptionKey";
    private static readonly ConcurrentDictionary<string, byte[]> KeyCache = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> UserLocks = new();

    public async Task<byte[]> GetOrCreateKeyAsync(string userId)
    {
        var auth0UserId = userId.StartsWith("auth0|", StringComparison.Ordinal)
            ? userId
            : $"auth0|{userId}";

        if (KeyCache.TryGetValue(auth0UserId, out var cachedKey))
        {
            return cachedKey.ToArray();
        }

        var userLock = UserLocks.GetOrAdd(auth0UserId, _ => new SemaphoreSlim(1, 1));
        await userLock.WaitAsync();
        try
        {
            if (KeyCache.TryGetValue(auth0UserId, out cachedKey))
            {
                return cachedKey.ToArray();
            }

            var user = await managementApiClient.Users.GetAsync(auth0UserId, new());
            var appMetadata = user.AppMetadata == null
                ? new Dictionary<string, object>()
                : user.AppMetadata.ToDictionary(item => item.Key, item => item.Value);

            if (appMetadata.TryGetValue(MetadataKey, out var value))
            {
                if (value is not string encodedKey || !TryDecodeKey(encodedKey, out var existingKey))
                {
                    throw new CryptographicException($"The stored encryption key for Auth0 user '{auth0UserId}' is invalid.");
                }

                KeyCache[auth0UserId] = existingKey;
                return existingKey.ToArray();
            }

            var key = RandomNumberGenerator.GetBytes(32);
            appMetadata[MetadataKey] = Convert.ToBase64String(key);

            await managementApiClient.Users.UpdateAsync(auth0UserId, new()
            {
                AppMetadata = appMetadata
            });

            KeyCache[auth0UserId] = key;
            return key.ToArray();
        }
        finally
        {
            userLock.Release();
        }
    }

    private static bool TryDecodeKey(string value, out byte[] key)
    {
        try
        {
            key = Convert.FromBase64String(value);
            return key.Length == 32;
        }
        catch (FormatException)
        {
            key = [];
            return false;
        }
    }
}
