using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using BabyTracker.Models.Database;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Services;

public interface IPictureService
{
    Task<byte[]?> GetPicture(IHostEnvironment hostEnvironment, ClaimsPrincipal user, string fileName);

    Task<byte[]?> GetPicture(IHostEnvironment hostEnvironment, string userId, string fileName);

    List<string> CheckPictures(IHostEnvironment hostEnvironment, string userId, List<Picture> pictures);
}

public class PictureService : IPictureService
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

        return await File.ReadAllBytesAsync(picturePath);
    }

    public List<string> CheckPictures(IHostEnvironment hostEnvironment, string userId, List<Picture> pictures)
    {
        var errors = new List<string>();

        foreach (var picture in pictures)
        {
            var picturePath = GetPicturePath(hostEnvironment, userId, picture.FileName);
            if (!File.Exists(picturePath))
            {
                errors.Add($"Missing {picturePath}");
            }
        }

        return errors;
    }

    private static string GetPicturePath(IHostEnvironment hostEnvironment, string userId, string fileName)
    {
        var basePath = hostEnvironment.IsProduction() ? "/data/Data" : $"{hostEnvironment.ContentRootPath}/Data";
        var picturePath = Path.Combine(basePath, userId, $"{fileName}.jpg");
        return picturePath;
    }
}
