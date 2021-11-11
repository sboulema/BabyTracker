using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BabyTracker.Services;

public interface IPictureService
{
    Task<byte[]> GetPicture(ClaimsPrincipal user, string fileName);
}

public class PictureService : IPictureService
{
    private readonly IAccountService _accountService;

    public PictureService(IAccountService accountService)
    {
        _accountService = accountService;
    }

    public async Task<byte[]> GetPicture(ClaimsPrincipal user, string fileName)
    {
        var profile = await _accountService.GetProfile(user);

        var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", profile?.UserId, $"{fileName}.jpg");

        if (!File.Exists(path))
        {
            path = $"/data/Data/{profile?.UserId}/{fileName}.jpg";
        }

        return await File.ReadAllBytesAsync(path);
    }
}
