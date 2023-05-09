using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public interface IImportService
{
    string Unzip(ClaimsPrincipal user, Stream stream);

    bool HasDataClone(ClaimsPrincipal user);
}

public class ImportService : IImportService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ImportService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public string Unzip(ClaimsPrincipal user, Stream stream)
    {
        var extractPath = GetDataClonePath(user);

        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }

        using var archive = new ZipArchive(stream);
        archive.ExtractToDirectory(extractPath, true);
        archive.Dispose();

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
