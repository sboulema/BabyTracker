using System;
using System.IO;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BabyTracker.Services;

public interface IPictureService
{
    Task<byte[]?> GetPicture(ClaimsPrincipal user, string fileName);

    Task<byte[]?> GetPicture(string userId, string fileName);

    /// <summary>Encrypts a JPEG straight into the clone directory of <paramref name="userId"/>.</summary>
    Task SavePictureAsync(string userId, string fileName, byte[] jpeg);

    /// <summary>Removes a picture file, for example after a failed save of the entry it belongs to.</summary>
    void DeletePicture(string userId, string fileName);
}

public class PictureService(
    IDataClonePathProvider pathProvider,
    IUserDataKeyService userDataKeyService,
    IDataEncryptionService dataEncryptionService,
    ILogger<PictureService> logger) : IPictureService
{
    public async Task<byte[]?> GetPicture(ClaimsPrincipal user, string fileName)
    {
        var activeUserId = user.FindFirstValue("activeUserId");

        if (string.IsNullOrEmpty(activeUserId))
        {
            return null;
        }

        return await GetPicture(activeUserId, fileName);
    }

    public async Task SavePictureAsync(string userId, string fileName, byte[] jpeg)
    {
        if (!pathProvider.TryGetPicturePath(userId, fileName, out var picturePath))
        {
            throw new ArgumentException("The picture file name is not valid.", nameof(fileName));
        }

        byte[]? key = null;

        try
        {
            key = await userDataKeyService.GetRequiredKeyAsync(userId);

            using var stream = new MemoryStream(jpeg);

            dataEncryptionService.EncryptStream(
                stream,
                picturePath,
                key,
                pathProvider.GetAssociatedData(userId, $"{fileName}.jpg"));
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }

    public void DeletePicture(string userId, string fileName)
    {
        if (pathProvider.TryGetPicturePath(userId, fileName, out var picturePath) &&
            File.Exists(picturePath))
        {
            File.Delete(picturePath);
        }
    }

    public async Task<byte[]?> GetPicture(string userId, string fileName)
    {
        // Rejects ids/names with separators or ".." so a request cannot escape the clone directory.
        if (!pathProvider.TryGetPicturePath(userId, fileName, out var picturePath) ||
            !File.Exists(picturePath))
        {
            return null;
        }

        byte[]? key = null;

        try
        {
            // Read path: never creates a key.
            key = await userDataKeyService.GetRequiredKeyAsync(userId);

            var encryptedContent = await File.ReadAllBytesAsync(picturePath);

            return dataEncryptionService.DecryptFile(
                encryptedContent,
                key,
                pathProvider.GetAssociatedData(userId, $"{fileName}.jpg"));
        }
        catch (CryptographicException e)
        {
            logger.LogError(e, "Could not decrypt picture '{FileName}' of user {UserId}", fileName, userId);

            return null;
        }
        finally
        {
            if (key is not null)
            {
                CryptographicOperations.ZeroMemory(key);
            }
        }
    }
}
