using MediBook.Configuration;
using Microsoft.Maui.Graphics;

namespace MediBook.Helpers;

public static class ImageCompressor
{
    public static async Task<Stream?> CompressImageAsync(string imageFilePath)
    {
        if (!File.Exists(imageFilePath))
            return null;

        var imageBytes = await File.ReadAllBytesAsync(imageFilePath);
        var compressed = ResizeAndCompress(imageBytes, 1024, 0.75f);
        if (compressed != null)
        {
            return new MemoryStream(compressed);
        }

        return File.OpenRead(imageFilePath);
    }

    public static async Task<Stream?> CompressImageStreamAsync(Stream inputStream)
    {
        using var memoryBuffer = new MemoryStream();
        await inputStream.CopyToAsync(memoryBuffer);
        var imageBytes = memoryBuffer.ToArray();

        // Target maximum dimension of 1024 for standard image streams
        var compressed = ResizeAndCompress(imageBytes, 1024, 0.75f);
        if (compressed != null)
        {
            return new MemoryStream(compressed);
        }

        return new MemoryStream(imageBytes);
    }

    public static byte[]? ResizeAndCompress(byte[] imageBytes, float maxDimension, float quality)
    {
        try
        {
            using var ms = new MemoryStream(imageBytes);
            var image = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(ms);
            if (image == null) return null;

            // Downsize to target dimension keeping aspect ratio
            using var resizedImage = image.Downsize(maxDimension, disposeOriginal: true);
            if (resizedImage == null) return null;

            return resizedImage.AsBytes(ImageFormat.Jpeg, quality);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ImageCompressor] ResizeAndCompress failed: {ex.Message}");
            return null;
        }
    }

    public static string GetMimeType(string imageFilePath)
    {
        var extension = Path.GetExtension(imageFilePath).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }

    public static string GenerateStoragePath(string userId, string folder, string fileName)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var extension = Path.GetExtension(fileName);
        var cleanedName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_")
            .Replace("/", "_");
        return $"{folder}/{userId}/{timestamp}_{cleanedName}{extension}";
    }
}
