using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BabyTracker.Services;

/// <remarks>
/// File layout: "BTENC2" | 12-byte nonce | 16-byte tag | ciphertext.
/// AES-256-GCM; the associated data is the magic plus a caller-supplied context (user + relative path).
/// </remarks>
public sealed class DataEncryptionService : IDataEncryptionService
{
    private static readonly byte[] Magic = "BTENC2"u8.ToArray();
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly int HeaderSize = Magic.Length + NonceSize + TagSize;

    public bool IsEncrypted(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        if (stream.Length < HeaderSize)
        {
            return false;
        }

        Span<byte> header = stackalloc byte[Magic.Length];
        var read = stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

        return read == header.Length && header.SequenceEqual(Magic);
    }

    public void EncryptFile(string path, byte[] key, string associatedData)
    {
        if (IsEncrypted(path))
        {
            return;
        }

        var plaintext = File.ReadAllBytes(path);

        try
        {
            WriteAtomically(path, Encrypt(plaintext, key, associatedData));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
        }
    }

    public void EncryptStream(Stream plaintext, string destinationPath, byte[] key, string associatedData)
    {
        using var buffer = new MemoryStream();
        plaintext.CopyTo(buffer);

        var plaintextBytes = buffer.GetBuffer();

        try
        {
            var encrypted = Encrypt(plaintextBytes.AsSpan(0, (int)buffer.Length), key, associatedData);

            Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            WriteAtomically(destinationPath, encrypted);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
        }
    }

    public byte[] DecryptFile(byte[] encryptedContent, byte[] key, string associatedData)
    {
        if (!HasHeader(encryptedContent))
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

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, BuildAssociatedData(associatedData));
            return plaintext;
        }
        catch
        {
            CryptographicOperations.ZeroMemory(plaintext);
            throw;
        }
    }

    /// <summary>Single allocation: header and ciphertext are written straight into the output buffer.</summary>
    private static byte[] Encrypt(ReadOnlySpan<byte> plaintext, byte[] key, string associatedData)
    {
        var encrypted = new byte[HeaderSize + plaintext.Length];

        Magic.CopyTo(encrypted, 0);

        var nonce = encrypted.AsSpan(Magic.Length, NonceSize);
        var tag = encrypted.AsSpan(Magic.Length + NonceSize, TagSize);
        var ciphertext = encrypted.AsSpan(HeaderSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, BuildAssociatedData(associatedData));

        return encrypted;
    }

    private static void WriteAtomically(string path, byte[] content)
    {
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";

        try
        {
            File.WriteAllBytes(temporaryPath, content);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static byte[] BuildAssociatedData(string associatedData)
    {
        var context = Encoding.UTF8.GetBytes(associatedData);
        var result = new byte[Magic.Length + context.Length];

        Magic.CopyTo(result, 0);
        context.CopyTo(result, Magic.Length);

        return result;
    }

    private static bool HasHeader(ReadOnlySpan<byte> content)
        => content.Length >= HeaderSize && content[..Magic.Length].SequenceEqual(Magic);
}
