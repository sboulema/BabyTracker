using System.IO;
using System.Threading.Tasks;

namespace BabyTracker.Services
{
    public static class PictureService
    {
        public static async Task<byte[]> GetPicture(string babyName, string fileName)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", babyName, $"{fileName}.jpg");

            if (!File.Exists(path)) {
                path = $"/data/Data/{babyName}/{fileName}.jpg";
            }

            return await File.ReadAllBytesAsync(path);
        }
    }
}
