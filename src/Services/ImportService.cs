using System;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BabyTracker.Services;

public interface IImportService
{
    Task<string> Unzip(ClaimsPrincipal user, Stream stream);

    bool HasDataClone(ClaimsPrincipal user);
}

public class ImportService(IWebHostEnvironment webHostEnvironment,
    IOutputCacheStore outputCacheStore,
    ILogger<ImportService> logger) : IImportService
{
    public async Task<string> Unzip(ClaimsPrincipal user, Stream stream)
    {
        var extractPath = GetDataClonePath(user);

        try
        {
            if (Directory.Exists(extractPath))
            {
                Directory.Delete(extractPath, true);
            }
        }
        catch (Exception e)
        {
            logger.LogError($"Error removing data clone path '{extractPath}'. {e.Message}");
        }

        try
        {
            using var archive = new ZipArchive(stream);
            archive.ExtractToDirectory(extractPath, true);
        }
        catch (Exception e)
        {
            logger.LogError($"Error extracting data into clone path '{extractPath}'. {e.Message}");
        }

        await outputCacheStore.EvictByTagAsync(user.FindFirstValue(ClaimTypes.NameIdentifier), default);

        return extractPath;
    }

    public bool HasDataClone(ClaimsPrincipal user)
        => Directory.Exists(GetDataClonePath(user));

    private string GetDataClonePath(ClaimsPrincipal user)
    {
        var activeUserId = user.FindFirstValue("activeUserId");

        var path = $"/data/Data/{activeUserId}";

        if (!webHostEnvironment.IsProduction())
        {
            path = $"{webHostEnvironment.ContentRootPath}/Data/{activeUserId}";
        }

        return path;
    }
}
