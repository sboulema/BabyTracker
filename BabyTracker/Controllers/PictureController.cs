using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authorization;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class PictureController : Controller
{
    [Authorize]
    [Route("{fileName}")]
    public async Task<IActionResult> GetPicture(string fileName)
    {
        var picture = await PictureService.GetPicture(User, fileName);

        return File(picture, "image/jpg");
    }

    [Authorize]
    [Route("{filename}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string fileName)
    {
        var thumbnail = await PictureService.GetPicture(User, $"{fileName}__thumbnail");

        return File(thumbnail, "image/jpg");
    }
}
