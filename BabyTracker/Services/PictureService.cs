using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BabyTracker.Services;

public static class PictureService
{
    public static async Task<byte[]> GetPicture(ClaimsPrincipal user, string fileName)
    {
        var profile = AccountService.GetProfile(user);

        var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", profile?.UserId, $"{fileName}.jpg");

        if (!File.Exists(path))
        {
            path = $"/data/Data/{profile?.UserId}/{fileName}.jpg";
        }

        return await File.ReadAllBytesAsync(path);
    }
}
