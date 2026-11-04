using System.IO;

namespace BabyTracker.Services;

public interface IDataEncryptionService
{
    /// <summary>Checks only the file header; does not read the whole file.</summary>
    bool IsEncrypted(string path);

    /// <summary>Encrypts a plaintext file in place (atomically). Files that are already encrypted are left alone.</summary>
    void EncryptFile(string path, byte[] key, string associatedData);

    /// <summary>
    /// Encrypts the stream straight into <paramref name="destinationPath"/>, so the plaintext
    /// never touches the disk. Always encrypts; it does not look for an existing header.
    /// </summary>
    void EncryptStream(Stream plaintext, string destinationPath, byte[] key, string associatedData);

    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// The content is not encrypted, the key is wrong, or the content/associated data was tampered with.
    /// </exception>
    byte[] DecryptFile(byte[] encryptedContent, byte[] key, string associatedData);
}
