using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using BabyTracker.Services;

namespace BabyTracker.Controllers
{
    [Route("[controller]")]
    public class PictureController : Controller
    {
        [Route("{filename}")]
        public async Task<IActionResult> GetPicture(string filename)
        {
            var picture = await PictureService.GetPicture(filename);

            return File(picture, "image/jpg");
        }

        [Route("{filename}/thumbnail")]
        public async Task<IActionResult> GetThumbnail(string filename)
        {
            var thumbnail = await PictureService.GetPicture($"{filename}__thumbnail");

            return File(thumbnail, "image/jpg");
        }
    }
}
