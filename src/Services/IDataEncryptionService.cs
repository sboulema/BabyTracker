namespace BabyTracker.Services;

public interface IDataEncryptionService
{
    void EncryptFile(string path, byte[] key);
    byte[] DecryptFile(byte[] encryptedContent, byte[] key);
    void DecryptFile(string encryptedPath, string plaintextPath, byte[] key);
}
