using System.IO;
using System.Threading.Tasks;

namespace BabyTracker.Services
{
    public static class PictureService
    {
        public static async Task<byte[]> GetPicture(string filename)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "zip", filename);

            if (!File.Exists(path)) {
                path = $"/data/zip/{filename}";
            }

            return await File.ReadAllBytesAsync($"{path}.jpg");
        }
    }
}
