using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace BabyTracker.Services;

public sealed record ProcessedPicture(byte[] Jpeg, byte[] Thumbnail);

public interface IImageService
{
    /// <summary>
    /// Turns an uploaded image into a JPEG and a JPEG thumbnail: rotated upright, shrunk to a sensible
    /// size and stripped of metadata such as the GPS location.
    /// </summary>
    /// <returns>null when the file is not a supported image.</returns>
    Task<ProcessedPicture?> CreateJpegAndThumbnailAsync(IFormFile file);
}

public sealed class ImageService : IImageService
{
    private const long MaxPixels = 80_000_000;
    private const int MaxSide = 2560;
    private const int ThumbnailSide = 512;

    public async Task<ProcessedPicture?> CreateJpegAndThumbnailAsync(IFormFile file)
    {
        try
        {
            await using var stream = file.OpenReadStream();

            // Look at the header first, so a decompression bomb is rejected before it is decoded
            var info = await Image.IdentifyAsync(stream);

            if (info.Width * (long)info.Height > MaxPixels)
            {
                return null;
            }

            stream.Position = 0;

            using var image = await Image.LoadAsync(stream);

            image.Mutate(context => context.AutoOrient());

            image.Metadata.ExifProfile = null;
            image.Metadata.XmpProfile = null;
            image.Metadata.IptcProfile = null;

            return new ProcessedPicture(
                await EncodeAsync(image, MaxSide, 85),
                await EncodeAsync(image, ThumbnailSide, 75));
        }
        catch (Exception e) when (e is UnknownImageFormatException or InvalidImageContentException or NotSupportedException)
        {
            return null;
        }
    }

    private static async Task<byte[]> EncodeAsync(Image image, int maxSide, int quality)
    {
        using var copy = image.Clone(context =>
        {
            // Only ever shrink
            if (Math.Max(image.Width, image.Height) > maxSide)
            {
                context.Resize(new ResizeOptions
                {
                    Size = new Size(maxSide, maxSide),
                    Mode = ResizeMode.Max
                });
            }
        });

        using var output = new MemoryStream();

        await copy.SaveAsJpegAsync(output, new JpegEncoder { Quality = quality });

        return output.ToArray();
    }
}
