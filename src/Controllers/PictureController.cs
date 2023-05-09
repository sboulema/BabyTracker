using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class PictureController : Controller
{
    private readonly IPictureService _pictureService;
    private readonly IHostEnvironment _hostEnvironment;

    public PictureController(IPictureService pictureService,
        IHostEnvironment hostEnvironment)
    {
        _pictureService = pictureService;
        _hostEnvironment = hostEnvironment;
    }

    [Authorize]
    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetPicture(string fileName)
    {
        var picture = await _pictureService.GetPicture(_hostEnvironment, User, fileName);

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
        var thumbnail = await _pictureService.GetPicture(_hostEnvironment, User, $"{fileName}__thumbnail");

        if (thumbnail == null)
        {
            return NotFound();
        }

        return File(thumbnail, "image/jpg");
    }

    [HttpGet("{userId}/{fileName}")]
    public async Task<IActionResult> GetPicture(string userId, string fileName)
    {
        var picture = await _pictureService.GetPicture(_hostEnvironment, userId, fileName);

        return File(picture, "image/jpg");
    }

    [HttpGet("{userId}/{filename}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string userId, string fileName)
    {
        var thumbnail = await _pictureService.GetPicture(_hostEnvironment, userId, $"{fileName}__thumbnail");

        return File(thumbnail, "image/jpg");
    }
}
