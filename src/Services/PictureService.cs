using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public interface IPictureService
{
    Task<byte[]?> GetPicture(IHostEnvironment hostEnvironment, ClaimsPrincipal user, string fileName);

    Task<byte[]?> GetPicture(IHostEnvironment hostEnvironment, string userId, string fileName);
}

public class PictureService(
    IUserDataKeyService userDataKeyService,
    IDataEncryptionService dataEncryptionService) : IPictureService
{
    public async Task<byte[]?> GetPicture(IHostEnvironment hostEnvironment, ClaimsPrincipal user, string fileName)
    {
        var activeUserId = user.FindFirstValue("activeUserId");

        if (string.IsNullOrEmpty(activeUserId))
        {
            return null;
        }

        return await GetPicture(hostEnvironment, activeUserId, fileName);
    }

    public async Task<byte[]?> GetPicture(IHostEnvironment hostEnvironment, string userId, string fileName)
    {
        var picturePath = GetPicturePath(hostEnvironment, userId, fileName);

        if (!File.Exists(picturePath))
        {
            return null;
        }

        var key = await userDataKeyService.GetOrCreateKeyAsync(userId);
        try
        {
            // Also migrate legacy pictures when they are requested directly.
            dataEncryptionService.EncryptFile(picturePath, key);
            var encryptedContent = await File.ReadAllBytesAsync(picturePath);
            return dataEncryptionService.DecryptFile(encryptedContent, key);
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
        }
    }

    private static string GetPicturePath(IHostEnvironment hostEnvironment, string userId, string fileName)
    {
        var basePath = hostEnvironment.IsProduction() ? "/data/Data" : Path.Combine(hostEnvironment.ContentRootPath, "Data");
        return Path.Combine(basePath, userId, $"{fileName}.jpg");
    }
}
