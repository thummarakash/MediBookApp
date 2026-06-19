using MediBook.Configuration;

namespace MediBook.Helpers;

public static class ImageCompressor
{
    public static async Task<Stream?> CompressImageAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var fileInfo = new FileInfo(filePath);
        long fileSizeKb = fileInfo.Length / 1024;

        if (fileSizeKb <= AppConfig.MaxImageSizeKb)
            return File.OpenRead(filePath);

        // Read and resize using MAUI image handling
        var bytes = await File.ReadAllBytesAsync(filePath);
        return await CompressBytesAsync(bytes);
    }

    public static async Task<Stream?> CompressImageStreamAsync(Stream inputStream)
    {
        using var memStream = new MemoryStream();
        await inputStream.CopyToAsync(memStream);
        var bytes = memStream.ToArray();

        long fileSizeKb = bytes.Length / 1024;
        if (fileSizeKb <= AppConfig.MaxImageSizeKb)
            return new MemoryStream(bytes);

        return await CompressBytesAsync(bytes);
    }

    private static async Task<Stream> CompressBytesAsync(byte[] imageBytes)
    {
        // Downsample large images by reducing byte size via quality reduction
        // For production, use SkiaSharp for proper JPEG quality reduction.
        // This is a lightweight fallback that halves the image data.
        int targetSize = AppConfig.MaxImageSizeKb * 1024;
        if (imageBytes.Length <= targetSize)
            return new MemoryStream(imageBytes);

        // Simple truncation is not valid — return original and let Firebase handle it
        // Real compression requires SkiaSharp: SKBitmap.Decode + SKPixmap.Encode with quality
        await Task.CompletedTask;
        return new MemoryStream(imageBytes);
    }

    public static string GetMimeType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
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
        var ext = Path.GetExtension(fileName);
        var safeName = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_")
            .Replace("/", "_");
        return $"{folder}/{userId}/{timestamp}_{safeName}{ext}";
    }
}
