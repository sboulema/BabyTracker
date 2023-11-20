using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.OutputCaching;

namespace BabyTracker.Controllers;

[Route("[controller]")]
public class PictureController(IPictureService pictureService,
    IHostEnvironment hostEnvironment) : Controller
{
    [OutputCache(PolicyName = "AuthenticatedOutputCache")]
    [Authorize]
    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetPicture(string fileName)
    {
        var picture = await pictureService.GetPicture(hostEnvironment, User, fileName);

        if (picture == null)
        {
            return NotFound();
        }

        return File(picture, "image/jpg");
    }

    [OutputCache(PolicyName = "AuthenticatedOutputCache")]
    [Authorize]
    [HttpGet("{filename}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string fileName)
    {
        var thumbnail = await pictureService.GetPicture(hostEnvironment, User, $"{fileName}__thumbnail");

        if (thumbnail == null)
        {
            return NotFound();
        }

        return File(thumbnail, "image/jpg");
    }

    [HttpGet("{userId}/{fileName}")]
    public async Task<IActionResult> GetPicture(string userId, string fileName)
    {
        var picture = await pictureService.GetPicture(hostEnvironment, userId, fileName);

        if (picture == null)
        {
            return NotFound();
        }

        return File(picture, "image/jpg");
    }

    [HttpGet("{userId}/{filename}/thumbnail")]
    public async Task<IActionResult> GetThumbnail(string userId, string fileName)
    {
        var thumbnail = await pictureService.GetPicture(hostEnvironment, userId, $"{fileName}__thumbnail");

        if (thumbnail == null)
        {
            return NotFound();
        }

        return File(thumbnail, "image/jpg");
    }
}
