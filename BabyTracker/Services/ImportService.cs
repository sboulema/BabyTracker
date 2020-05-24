using BabyTracker.Models;
using CsvHelper;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace BabyTracker.Services
{
    public interface IImportService
    {
        ImportResultModel HandleImport(IFormFile file);

        ImportResultModel LoadFromZip(string fileName);
    }

    public class ImportService : IImportService
    {
        public ImportResultModel LoadFromZip(string fileName)
        {
            var path = $"/data/{fileName}.eml";
            return ParseZip(path);
        }

        public ImportResultModel HandleImport(IFormFile file)
        {
            var path = SaveFile(file);
            return ParseZip(path);
        }

        private ImportResultModel ParseZip(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var extractDir = Unzip(path);

            var model = new ImportResultModel();

            if (Directory.GetFiles(extractDir).Any(f => f.Contains("EasyLog.db")))
            {
                model.Entries = SqLiteService.ParseDb(extractDir);
            }
            else
            {
                model.Entries = ParseFiles(extractDir);
            }

            return model;
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

        private static string Unzip(string path)
        {
            var extractPath = Path.Combine(Path.GetDirectoryName(path), "zip");

            using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Read);
            archive.ExtractToDirectory(extractPath, true);

            return extractPath;
        }

        private static List<EntryModel> ParseFiles(string path)
        {
            var entries = new List<EntryModel>();

            var files = Directory.GetFiles(path);

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);

                if (fileName.EndsWith("diaper"))
                {
                    entries.AddRange(ParseCsv<Diaper>(file));
                }
                else if (fileName.EndsWith("formula"))
                {
                    entries.AddRange(ParseCsv<Formula>(file));
                }
                else if (fileName.EndsWith("growth"))
                {
                    entries.AddRange(ParseCsv<Growth>(file));
                }
                else if (fileName.EndsWith("medication"))
                {
                    entries.AddRange(ParseCsv<MedicationModel>(file));
                }
                else if (fileName.EndsWith("milestone"))
                {
                    entries.AddRange(ParseCsv<MilestoneModel>(file));
                }
                else if (fileName.EndsWith("other_activity"))
                {
                    entries.AddRange(ParseCsv<ActivityModel>(file));
                }
                else if (fileName.EndsWith("sleep"))
                {
                    entries.AddRange(ParseCsv<SleepModel>(file));
                }
                else if (fileName.EndsWith("supplement"))
                {
                    entries.AddRange(ParseCsv<SupplementModel>(file));
                }
                else if (fileName.EndsWith("temperature"))
                {
                    entries.AddRange(ParseCsv<TemperatureModel>(file));
                }
                else if (fileName.EndsWith("vaccine"))
                {
                    entries.AddRange(ParseCsv<VaccineModel>(file));
                }
            }

            return entries;
        }

        private static IEnumerable<T> ParseCsv<T>(string path)
        {
            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
            return csv.GetRecords<T>().ToList();
        }
    }
}
