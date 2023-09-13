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

public class ImportService : IImportService
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IOutputCacheStore _outputCacheStore;
    private readonly ILogger<ImportService> _logger;

    public ImportService(IWebHostEnvironment webHostEnvironment,
        IOutputCacheStore outputCacheStore,
        ILogger<ImportService> logger)
    {
        _webHostEnvironment = webHostEnvironment;
        _outputCacheStore = outputCacheStore;
        _logger = logger;
    }

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
            _logger.LogError($"Error removing data clone path '{extractPath}'. {e.Message}");
        }

        try
        {
            using var archive = new ZipArchive(stream);
            archive.ExtractToDirectory(extractPath, true);
        }
        catch (Exception e)
        {
            _logger.LogError($"Error extracting data into clone path '{extractPath}'. {e.Message}");
        }

        await _outputCacheStore.EvictByTagAsync(user.FindFirstValue(ClaimTypes.NameIdentifier), default);

        return extractPath;
    }

    public bool HasDataClone(ClaimsPrincipal user)
        => Directory.Exists(GetDataClonePath(user));

    private string GetDataClonePath(ClaimsPrincipal user)
    {
        var activeUserId = user.FindFirstValue("activeUserId");

        var path = $"/data/Data/{activeUserId}";

        if (!_webHostEnvironment.IsProduction())
        {
            path = $"{_webHostEnvironment.ContentRootPath}/Data/{activeUserId}";
        }

        return path;
    }
}
