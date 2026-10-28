using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Auth0.ManagementApi;
using Microsoft.Data.Sqlite;

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
    void EncryptSqliteDatabase(string path, byte[] key);
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
        File.WriteAllBytes(temporaryPath, encrypted);
        File.Move(temporaryPath, path, true);
        CryptographicOperations.ZeroMemory(plaintext);
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

    public void EncryptSqliteDatabase(string path, byte[] key)
    {
        var encryptedPath = path + ".encrypted";
        if (File.Exists(encryptedPath))
        {
            File.Delete(encryptedPath);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWrite
        }.ToString();

        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();

            using var command = connection.CreateCommand();
            command.CommandText = """
                ATTACH DATABASE $encryptedPath AS encrypted KEY $key;
                SELECT sqlcipher_export('encrypted');
                DETACH DATABASE encrypted;
                """;
            command.Parameters.AddWithValue("$encryptedPath", encryptedPath);
            command.Parameters.AddWithValue("$key", Convert.ToBase64String(key));
            command.ExecuteNonQuery();
        }

        File.Move(encryptedPath, path, true);
    }

    private static bool IsEncrypted(ReadOnlySpan<byte> content)
        => content.Length >= Magic.Length &&
           content[..Magic.Length].SequenceEqual(Magic);
}
