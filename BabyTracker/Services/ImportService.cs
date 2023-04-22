using System.IO;
using System.IO.Compression;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public interface IImportService
{
    Task<string> Unzip(Stream stream);

    Task<bool> HasDataClone();
}

public class ImportService : IImportService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ImportService(IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment webHostEnvironment)
    {
        _httpContextAccessor = httpContextAccessor;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<string> Unzip(Stream stream)
    {
        var extractPath = await GetDataClonePath();

        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }

        using var archive = new ZipArchive(stream);
        archive.ExtractToDirectory(extractPath, true);
        archive.Dispose();

        return extractPath;
    }

    public async Task<bool> HasDataClone()
    {
        var path = await GetDataClonePath();

        return Directory.Exists(path);
    }

    private async Task<string> GetDataClonePath()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null)
        {
            return string.Empty;
        }

        var activeUserId = user.FindFirstValue("activeUserId");

        var path = $"/data/Data/{activeUserId}";

        if (!_webHostEnvironment.IsProduction())
        {
            path = $"{_webHostEnvironment.ContentRootPath}/Data/{activeUserId}";
        }

        return path;
    }
}
