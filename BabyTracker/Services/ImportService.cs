using System.IO;
using System.IO.Compression;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public interface IImportService
{
    string Unzip(Stream stream);

    bool HasDataClone();
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

    public string Unzip(Stream stream)
    {
        var extractPath = GetDataClonePath();

        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }

        using var archive = new ZipArchive(stream);
        archive.ExtractToDirectory(extractPath, true);
        archive.Dispose();

        return extractPath;
    }

    public bool HasDataClone()
    {
        var path = GetDataClonePath();

        return Directory.Exists(path);
    }

    private string GetDataClonePath()
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user == null)
        {
            return string.Empty;
        }

        var profile = AccountService.GetProfile(user);

        var path = $"/data/Data/{profile?.UserId}";

        if (!_webHostEnvironment.IsProduction())
        {
            path = $"{_webHostEnvironment.ContentRootPath}/Data/{profile?.UserId}";
        }

        return path;
    }
}
