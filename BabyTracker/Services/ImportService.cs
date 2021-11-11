using System.IO;
using System.IO.Compression;
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
    private readonly IAccountService _accountService;

    public ImportService(IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment webHostEnvironment,
        IAccountService accountService)
    {
        _httpContextAccessor = httpContextAccessor;
        _webHostEnvironment = webHostEnvironment;
        _accountService = accountService;
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

        var profile = await _accountService.GetProfile(user);

        var path = $"/data/Data/{profile?.UserId}";

        if (!_webHostEnvironment.IsProduction())
        {
            path = $"{_webHostEnvironment.ContentRootPath}/Data/{profile?.UserId}";
        }

        return path;
    }
}
