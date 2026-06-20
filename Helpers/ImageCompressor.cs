using MediBook.Configuration;

namespace MediBook.Helpers;

public static class ImageCompressor
{
    public static async Task<Stream?> CompressImageAsync(string img_filepath)
    {
        if (!File.Exists(img_filepath))
            return null;

        var img_info = new FileInfo(img_filepath);
        long kb_size = img_info.Length / 1024;

        if (kb_size <= AppConfig.MaxImageSizeKb)
            return File.OpenRead(img_filepath);

        var byte_data = await File.ReadAllBytesAsync(img_filepath);
        return await CompressBytesAsync(byte_data);
    }

    public static async Task<Stream?> CompressImageStreamAsync(Stream inputStream)
    {
        using var memory_buffer = new MemoryStream();
        await inputStream.CopyToAsync(memory_buffer);
        var byte_data = memory_buffer.ToArray();

        long kb_size = byte_data.Length / 1024;
        if (kb_size <= AppConfig.MaxImageSizeKb)
            return new MemoryStream(byte_data);

        return await CompressBytesAsync(byte_data);
    }

    private static async Task<Stream> CompressBytesAsync(byte[] imageBytes)
    {
        int targetSize = AppConfig.MaxImageSizeKb * 1024;
        if (imageBytes.Length <= targetSize)
            return new MemoryStream(imageBytes);

        await Task.CompletedTask;
        return new MemoryStream(imageBytes);
    }

    public static string GetMimeType(string img_filepath)
    {
        var file_extension = Path.GetExtension(img_filepath).ToLowerInvariant();
        return file_extension switch
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
        var file_extension = Path.GetExtension(fileName);
        var cleaned_filename = Path.GetFileNameWithoutExtension(fileName)
            .Replace(" ", "_")
            .Replace("/", "_");
        return $"{folder}/{userId}/{timestamp}_{cleaned_filename}{file_extension}";
    }
}
