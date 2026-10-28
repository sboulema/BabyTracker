using System;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public interface IImportService
{
    Task<string> Unzip(ClaimsPrincipal user, Stream stream);

    bool HasDataClone(ClaimsPrincipal user);
}

public class ImportService(IWebHostEnvironment webHostEnvironment,
    IOutputCacheStore outputCacheStore,
    IUserDataKeyService userDataKeyService,
    IDataEncryptionService dataEncryptionService) : IImportService
{
    public async Task<string> Unzip(ClaimsPrincipal user, Stream stream)
    {
        var activeUserId = user.FindFirstValue("activeUserId");
        if (string.IsNullOrWhiteSpace(activeUserId))
        {
            throw new InvalidOperationException("No active user is available for this import.");
        }

        var extractPath = GetDataClonePath(activeUserId);

        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }

        Directory.CreateDirectory(extractPath);

        try
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
            {
                archive.ExtractToDirectory(extractPath, overwriteFiles: true);
            }

            var key = await userDataKeyService.GetOrCreateKeyAsync(activeUserId);
            try
            {
                foreach (var file in Directory.EnumerateFiles(extractPath, "*", SearchOption.AllDirectories))
                {
                    dataEncryptionService.EncryptFile(file, key);
                }
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(key);
            }
        }
        catch
        {
            // Never leave an incomplete import containing plaintext files.
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }

            throw;
        }

        await outputCacheStore.EvictByTagAsync(user.FindFirstValue(ClaimTypes.NameIdentifier), default);

        return extractPath;
    }

    public bool HasDataClone(ClaimsPrincipal user)
    {
        var activeUserId = user.FindFirstValue("activeUserId");
        if (string.IsNullOrWhiteSpace(activeUserId))
        {
            return false;
        }

        var path = GetDataClonePath(activeUserId);
        return Directory.Exists(path) && File.Exists(Path.Combine(path, "EasyLog.db"));
    }

    private string GetDataClonePath(string activeUserId)
    {
        var path = $"/data/Data/{activeUserId}";

        if (!webHostEnvironment.IsProduction())
        {
            path = Path.Combine(webHostEnvironment.ContentRootPath, "Data", activeUserId);
        }

        return path;
    }
}
