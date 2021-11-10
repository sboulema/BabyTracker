using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class PictureController : Controller
{
    [Route("{babyName}/{fileName}")]
    public async Task<IActionResult> GetPicture(string babyName, string fileName)
    {
        var picture = await PictureService.GetPicture(babyName, fileName);

        return File(picture, "image/jpg");
    }

    [Route("{babyName}/{filename}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string babyName, string fileName)
    {
        var thumbnail = await PictureService.GetPicture(babyName, $"{fileName}__thumbnail");

        return File(thumbnail, "image/jpg");
    }
}
