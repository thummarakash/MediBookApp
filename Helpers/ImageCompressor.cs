using MediBook.Configuration;

namespace MediBook.Helpers;

public static class ImageCompressor
{
    public static async Task<Stream?> CompressImageAsync(string imageFilePath)
    {
        if (!File.Exists(imageFilePath))
            return null;

        var fileInfo = new FileInfo(imageFilePath);
        long fileSizeKb = fileInfo.Length / 1024;

        if (fileSizeKb <= AppConfig.MaxImageSizeKb)
            return File.OpenRead(imageFilePath);

        var imageBytes = await File.ReadAllBytesAsync(imageFilePath);
        return await CompressBytesAsync(imageBytes);
    }

    public static async Task<Stream?> CompressImageStreamAsync(Stream inputStream)
    {
        using var memoryBuffer = new MemoryStream();
        await inputStream.CopyToAsync(memoryBuffer);
        var imageBytes = memoryBuffer.ToArray();

        long fileSizeKb = imageBytes.Length / 1024;
        if (fileSizeKb <= AppConfig.MaxImageSizeKb)
            return new MemoryStream(imageBytes);

        return await CompressBytesAsync(imageBytes);
    }

    private static async Task<Stream> CompressBytesAsync(byte[] imageBytes)
    {
        int targetSize = AppConfig.MaxImageSizeKb * 1024;
        if (imageBytes.Length <= targetSize)
            return new MemoryStream(imageBytes);

        await Task.CompletedTask;
        return new MemoryStream(imageBytes);
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
