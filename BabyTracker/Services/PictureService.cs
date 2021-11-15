using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BabyTracker.Services;

public interface IPictureService
{
    Task<byte[]?> GetPicture(ClaimsPrincipal user, string fileName);

    Task<byte[]> GetPicture(string userId, string fileName);
}

public class PictureService : IPictureService
{
    private readonly IAccountService _accountService;

    public PictureService(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<byte[]?> GetPicture(ClaimsPrincipal user, string fileName)
    {
        var profile = await _accountService.GetProfile(user);

        if (profile == null)
        {
            return null;
        }

        return await GetPicture(profile.UserId, fileName);
    }

    public async Task<byte[]> GetPicture(string userId, string fileName)
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", userId, $"{fileName}.jpg");

        if (!File.Exists(path))
        {
            path = $"/data/Data/{userId}/{fileName}.jpg";
        }

        return await File.ReadAllBytesAsync(path);
    }
}
