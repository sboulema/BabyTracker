using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public interface IPictureService
{
    Task<byte[]?> GetPicture(IHostEnvironment hostEnvironment, ClaimsPrincipal user, string fileName);

    Task<byte[]> GetPicture(IHostEnvironment hostEnvironment, string userId, string fileName);
}

public class PictureService : IPictureService
{
    public async Task<byte[]?> GetPicture(IHostEnvironment hostEnvironment, ClaimsPrincipal user, string fileName)
    {
        var userId = user.FindFirstValue("userId");

        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        return await GetPicture(hostEnvironment, userId, fileName);
    }

    public async Task<byte[]> GetPicture(IHostEnvironment hostEnvironment, string userId, string fileName)
    {
        var basePath = hostEnvironment.IsProduction() ? "/data/Data" : $"{hostEnvironment.ContentRootPath}/Data";
        var picturePath = Path.Combine(basePath, userId, $"{fileName}.jpg");

        return await File.ReadAllBytesAsync(picturePath);
    }
}
