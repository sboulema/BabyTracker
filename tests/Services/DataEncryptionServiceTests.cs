using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using BabyTracker.Services;
using NUnit.Framework;

namespace BabyTracker.Tests.Services;

[TestFixture]
public class DataEncryptionServiceTests
{
    private string _testDirectory;
    private DataEncryptionService _encryptionService;

    [SetUp]
    public void SetUp()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _encryptionService = new DataEncryptionService();
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Directory.Delete(_testDirectory, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Test]
    public void IsEncrypted_WithUnencryptedFile_ReturnsFalse()
    {
        string filePath = Path.Combine(_testDirectory, "test.txt");
        File.WriteAllText(filePath, "Hello, World!");

        bool result = _encryptionService.IsEncrypted(filePath);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsEncrypted_WithShortFile_ReturnsFalse()
    {
        string filePath = Path.Combine(_testDirectory, "short.txt");
        File.WriteAllText(filePath, "Hi");

        bool result = _encryptionService.IsEncrypted(filePath);

        Assert.That(result, Is.False);
    }

    [Test]
    public void IsEncrypted_WithEncryptedFile_ReturnsTrue()
    {
        string filePath = Path.Combine(_testDirectory, "encrypted.bin");
        byte[] key = new byte[32]; // 256-bit key
        RandomNumberGenerator.Fill(key);

        // Create a simple encrypted file with the magic header
        byte[] encryptedContent = new byte[50];
        byte[] magic = Encoding.ASCII.GetBytes("BTENC2");
        magic.CopyTo(encryptedContent, 0);
        
        File.WriteAllBytes(filePath, encryptedContent);

        bool result = _encryptionService.IsEncrypted(filePath);

        Assert.That(result, Is.True);
    }

    [Test]
    public void EncryptFile_WithUnencryptedFile_CreatesEncryptedFile()
    {
        string filePath = Path.Combine(_testDirectory, "test.txt");
        string originalContent = "Hello, World!";
        File.WriteAllText(filePath, originalContent);

        byte[] key = new byte[32]; // 256-bit key
        RandomNumberGenerator.Fill(key);
        string associatedData = "user1|test.txt";

        _encryptionService.EncryptFile(filePath, key, associatedData);

        Assert.That(_encryptionService.IsEncrypted(filePath), Is.True);
    }

    [Test]
    public void EncryptFile_WithAlreadyEncryptedFile_DoesNotModify()
    {
        string filePath = Path.Combine(_testDirectory, "test.txt");
        string originalContent = "Hello, World!";
        File.WriteAllText(filePath, originalContent);

        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        string associatedData = "user1|test.txt";

        // Encrypt once
        _encryptionService.EncryptFile(filePath, key, associatedData);
        byte[] firstEncrypted = File.ReadAllBytes(filePath);

        // Try to encrypt again
        _encryptionService.EncryptFile(filePath, key, associatedData);
        byte[] secondEncrypted = File.ReadAllBytes(filePath);

        // Content should be the same (not re-encrypted)
        Assert.That(firstEncrypted, Is.EqualTo(secondEncrypted));
    }

    [Test]
    public void EncryptAndDecryptFile_RoundTrip()
    {
        string filePath = Path.Combine(_testDirectory, "test.txt");
        string originalContent = "Hello, World! This is a test.";
        File.WriteAllText(filePath, originalContent);

        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        string associatedData = "user1|test.txt";

        // Encrypt
        _encryptionService.EncryptFile(filePath, key, associatedData);

        // Read encrypted content
        byte[] encryptedContent = File.ReadAllBytes(filePath);

        // Decrypt
        byte[] decryptedContent = _encryptionService.DecryptFile(encryptedContent, key, associatedData);
        string decryptedText = Encoding.UTF8.GetString(decryptedContent);

        Assert.That(decryptedText, Is.EqualTo(originalContent));
    }

    [Test]
    public void DecryptFile_WithWrongKey_ThrowsException()
    {
        string filePath = Path.Combine(_testDirectory, "test.txt");
        string originalContent = "Hello, World!";
        File.WriteAllText(filePath, originalContent);

        byte[] correctKey = new byte[32];
        byte[] wrongKey = new byte[32];
        RandomNumberGenerator.Fill(correctKey);
        RandomNumberGenerator.Fill(wrongKey);
        string associatedData = "user1|test.txt";

        // Encrypt with correct key
        _encryptionService.EncryptFile(filePath, correctKey, associatedData);

        // Read encrypted content
        byte[] encryptedContent = File.ReadAllBytes(filePath);

        // Try to decrypt with wrong key
        Assert.Throws<CryptographicException>(() =>
            _encryptionService.DecryptFile(encryptedContent, wrongKey, associatedData));
    }

    [Test]
    public void DecryptFile_WithWrongAssociatedData_ThrowsException()
    {
        string filePath = Path.Combine(_testDirectory, "test.txt");
        string originalContent = "Hello, World!";
        File.WriteAllText(filePath, originalContent);

        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        string correctAssociatedData = "user1|test.txt";
        string wrongAssociatedData = "user2|test.txt";

        // Encrypt with correct associated data
        _encryptionService.EncryptFile(filePath, key, correctAssociatedData);

        // Read encrypted content
        byte[] encryptedContent = File.ReadAllBytes(filePath);

        // Try to decrypt with wrong associated data
        Assert.Throws<CryptographicException>(() =>
            _encryptionService.DecryptFile(encryptedContent, key, wrongAssociatedData));
    }

    [Test]
    public void DecryptFile_WithUnencryptedContent_ThrowsException()
    {
        string filePath = Path.Combine(_testDirectory, "test.txt");
        string content = "Hello, World!";
        File.WriteAllText(filePath, content);

        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        string associatedData = "user1|test.txt";

        byte[] unencryptedContent = File.ReadAllBytes(filePath);

        Assert.Throws<CryptographicException>(() =>
            _encryptionService.DecryptFile(unencryptedContent, key, associatedData));
    }

    [Test]
    public void EncryptStream_WithValidInput_CreatesEncryptedFile()
    {
        string destinationPath = Path.Combine(_testDirectory, "encrypted.bin");
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        string associatedData = "user1|stream.txt";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));

        _encryptionService.EncryptStream(stream, destinationPath, key, associatedData);

        Assert.That(File.Exists(destinationPath), Is.True);
        Assert.That(_encryptionService.IsEncrypted(destinationPath), Is.True);
    }

    [Test]
    public void EncryptStream_CreatesParentDirectoryIfNotExists()
    {
        string destinationPath = Path.Combine(_testDirectory, "subdir", "encrypted.bin");
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        string associatedData = "user1|stream.txt";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Test content"));

        _encryptionService.EncryptStream(stream, destinationPath, key, associatedData);

        Assert.That(File.Exists(destinationPath), Is.True);
    }

    [Test]
    public void EncryptAndDecryptStream_RoundTrip()
    {
        string destinationPath = Path.Combine(_testDirectory, "encrypted.bin");
        string originalContent = "Hello, World! This is a stream test.";
        byte[] key = new byte[32];
        RandomNumberGenerator.Fill(key);
        string associatedData = "user1|stream.txt";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));
        _encryptionService.EncryptStream(stream, destinationPath, key, associatedData);

        byte[] encryptedContent = File.ReadAllBytes(destinationPath);
        byte[] decryptedContent = _encryptionService.DecryptFile(encryptedContent, key, associatedData);
        string decryptedText = Encoding.UTF8.GetString(decryptedContent);

        Assert.That(decryptedText, Is.EqualTo(originalContent));
    }


}
