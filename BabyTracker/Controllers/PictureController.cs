using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class PictureController : Controller
{
    private readonly IPictureService _pictureService;

    public PictureController(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    [Route("{fileName}")]
    public async Task<IActionResult> GetPicture(string fileName)
    {
        var picture = await _pictureService.GetPicture(User, fileName);

        return File(picture, "image/jpg");
    }

    [Route("{filename}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string fileName)
    {
        var thumbnail = await _pictureService.GetPicture(User, $"{fileName}__thumbnail");

        return File(thumbnail, "image/jpg");
    }
}
