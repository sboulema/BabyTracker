using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Auth0.ManagementApi;

namespace BabyTracker.Services;

public interface IUserDataKeyService
{
    Task<byte[]> GetOrCreateKeyAsync(string userId);
}

public sealed class Auth0UserDataKeyService(IManagementApiClient managementApiClient) : IUserDataKeyService
{
    private const string MetadataKey = "BabyTrackerDataEncryptionKey";

    public async Task<byte[]> GetOrCreateKeyAsync(string userId)
    {
        var auth0UserId = userId.StartsWith("auth0|", StringComparison.Ordinal)
            ? userId
            : $"auth0|{userId}";

        var user = await managementApiClient.Users.GetAsync(auth0UserId, new());
        var appMetadata = user.AppMetadata == null
            ? new Dictionary<string, object>()
            : user.AppMetadata.ToDictionary(item => item.Key, item => item.Value);

        if (appMetadata.TryGetValue(MetadataKey, out var value) &&
            value is string encodedKey &&
            TryDecodeKey(encodedKey, out var existingKey))
        {
            return existingKey;
        }

        var key = RandomNumberGenerator.GetBytes(32);
        appMetadata[MetadataKey] = Convert.ToBase64String(key);

        await managementApiClient.Users.UpdateAsync(auth0UserId, new()
        {
            AppMetadata = appMetadata
        });

        return key;
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

public interface IDataEncryptionService
{
    void EncryptFile(string path, byte[] key);
    byte[] DecryptFile(byte[] encryptedContent, byte[] key);
    void DecryptFile(string encryptedPath, string plaintextPath, byte[] key);
}

public sealed class DataEncryptionService : IDataEncryptionService
{
    private static readonly byte[] Magic = "BTENC1"u8.ToArray();
    private const int NonceSize = 12;
    private const int TagSize = 16;

    public void EncryptFile(string path, byte[] key)
    {
        var plaintext = File.ReadAllBytes(path);

        if (IsEncrypted(plaintext))
        {
            CryptographicOperations.ZeroMemory(plaintext);
            return;
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using (var aes = new AesGcm(key, TagSize))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag, Magic);
        }

        var encrypted = new byte[Magic.Length + NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(Magic, 0, encrypted, 0, Magic.Length);
        Buffer.BlockCopy(nonce, 0, encrypted, Magic.Length, NonceSize);
        Buffer.BlockCopy(tag, 0, encrypted, Magic.Length + NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, encrypted, Magic.Length + NonceSize + TagSize, ciphertext.Length);

        var temporaryPath = path + ".encrypted";
        try
        {
            File.WriteAllBytes(temporaryPath, encrypted);
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(encrypted);
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    public byte[] DecryptFile(byte[] encryptedContent, byte[] key)
    {
        if (!IsEncrypted(encryptedContent))
        {
            throw new CryptographicException("The file is not encrypted with BabyTracker file encryption.");
        }

        var offset = Magic.Length;
        var nonce = encryptedContent.AsSpan(offset, NonceSize);
        offset += NonceSize;
        var tag = encryptedContent.AsSpan(offset, TagSize);
        offset += TagSize;
        var ciphertext = encryptedContent.AsSpan(offset);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, Magic);
        return plaintext;
    }

    public void DecryptFile(string encryptedPath, string plaintextPath, byte[] key)
    {
        var encrypted = File.ReadAllBytes(encryptedPath);
        var plaintext = DecryptFile(encrypted, key);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(plaintextPath)!);
            File.WriteAllBytes(plaintextPath, plaintext);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(encrypted);
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    private static bool IsEncrypted(ReadOnlySpan<byte> content)
        => content.Length >= Magic.Length + NonceSize + TagSize &&
           content[..Magic.Length].SequenceEqual(Magic);
}
