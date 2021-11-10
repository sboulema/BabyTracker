using Microsoft.AspNetCore.Http;
using System.IO;
using System.IO.Compression;
using System.Security.Claims;

namespace BabyTracker.Services;

public static class ImportService
{
    public static string HandleImport(IFormFile file, ClaimsPrincipal user)
    {
        // Save and extract Data Clone file
        var path = SaveFile(file);
        var extractPath = Unzip(path, user);

        // Delete Data Clone file
        File.Delete(path);

        // Return location of extracted Data Clone
        return extractPath;
    }

    /// <summary>
    /// Unzip Data Clone file to user specific folder
    /// </summary>
    /// <param name="path">Path to a Data Clone file</param>
    /// <param name="user">Logged in user</param>
    /// <returns></returns>
    private static string Unzip(string path, ClaimsPrincipal user)
    {
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var profile = AccountService.GetProfile(user);

        var extractPath = Path.Combine(Path.GetDirectoryName(path), "Data", profile?.UserId);

        if (Directory.Exists(extractPath))
        {
            Directory.Delete(extractPath, true);
        }

        using var archive = ZipFile.Open(path, ZipArchiveMode.Read);
        archive.ExtractToDirectory(extractPath, true);
        archive.Dispose();

        return extractPath;
    }

    /// <summary>
    /// Save uploaded Data Clone file to a file on the server
    /// </summary>
    /// <param name="file">Uploaded file</param>
    /// <returns></returns>
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

    public static bool HasDataClone(ClaimsPrincipal user, bool isProduction)
    {
        var profile = AccountService.GetProfile(user);

        var path = $"/data/Data/{profile?.UserId}";

        if (!isProduction)
        {
            path = $"/Data/{profile?.UserId}";
        }

        return Directory.Exists(path);
    }
}
