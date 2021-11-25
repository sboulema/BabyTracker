using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authorization;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class PictureController : Controller
{
    private readonly IPictureService _pictureService;

    public PictureController(IPictureService pictureService)
    {
        _pictureService = pictureService;
    }

    [Authorize]
    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetPicture(string fileName)
    {
        var picture = await _pictureService.GetPicture(User, fileName);

        if (picture == null)
        {
            return NotFound();
        }

        return File(picture, "image/jpg");
    }

    [Authorize]
    [HttpGet("{filename}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string fileName)
    {
        var thumbnail = await _pictureService.GetPicture(User, $"{fileName}__thumbnail");

        if (thumbnail == null)
        {
            return NotFound();
        }

        return File(thumbnail, "image/jpg");
    }

    [HttpGet("{userId}/{fileName}")]
    public async Task<IActionResult> GetPicture(string userId, string fileName)
    {
        var picture = await _pictureService.GetPicture(userId, fileName);

        return File(picture, "image/jpg");
    }

    [HttpGet("{userId}/{filename}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string userId, string fileName)
    {
        var thumbnail = await _pictureService.GetPicture(userId, $"{fileName}__thumbnail");

        return File(thumbnail, "image/jpg");
    }
}
