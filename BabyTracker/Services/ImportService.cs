using Microsoft.AspNetCore.Http;
using System.IO;
using System.IO.Compression;

namespace BabyTracker.Services
{
    public interface IImportService
    {
        string HandleImport(IFormFile file);

        string HandleLoad(string fileName);
    }

    public class ImportService : IImportService
    {
        public string HandleLoad(string fileName)
        {
            var path = $"/data/{fileName}.eml";
            return Unzip(path);
        }

        public string HandleImport(IFormFile file)
        {
            var path = SaveFile(file);
            return Unzip(path);
        }

        private string Unzip(string path)
        {
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            var extractPath = Path.Combine(Path.GetDirectoryName(path), "zip");

            using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Read);
            archive.ExtractToDirectory(extractPath, true);
            archive.Dispose();

            return extractPath;
        }

        private static string SaveFile(IFormFile file)
        {
            if (file == null)
            {
                return string.Empty;
            }

            var fileName = Path.GetFileName(file.FileName);

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            using var localFile = File.OpenWrite(fileName);
            using var uploadedFile = file.OpenReadStream();

            uploadedFile.CopyTo(localFile);

            return localFile.Name;
        }
    }
}
