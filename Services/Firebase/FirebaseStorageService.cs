using System.Net.Http.Headers;
using System.Text.Json;
using MediBook.Configuration;
using MediBook.Helpers;
using MediBook.Services.Auth;

namespace MediBook.Services.Firebase;

public class FirebaseStorageService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(60) };

    public static FirebaseStorageService Instance { get; } = new();
    private FirebaseStorageService() { }

    public event Action<double>? UploadProgressChanged;

    public async Task<string> UploadFileAsync(
        Stream fileStream,
        string storagePath,
        string mimeType,
        IProgress<double>? progress = null)
    {
        var encodedPath = Uri.EscapeDataString(storagePath);
        var url = $"{AppConfig.FirebaseStorageBaseUrl}/{encodedPath}?uploadType=media";

        var token = await SessionService.Instance.GetValidTokenAsync();

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        var downloadToken = doc.TryGetProperty("downloadTokens", out var dt) ? dt.GetString() : null;
        return BuildDownloadUrl(storagePath, downloadToken);
    }

    public async Task<string> UploadFileFromPathAsync(string localPath, string userId, string folder)
    {
        if (!File.Exists(localPath))
            throw new FileNotFoundException("Local file not found.", localPath);

        var mimeType = ImageCompressor.GetMimeType(localPath);
        var fileName = Path.GetFileName(localPath);
        var storagePath = ImageCompressor.GenerateStoragePath(userId, folder, fileName);

        Stream stream;
        if (mimeType.StartsWith("image/"))
        {
            var compressed = await ImageCompressor.CompressImageAsync(localPath);
            stream = compressed ?? File.OpenRead(localPath);
        }
        else
        {
            stream = File.OpenRead(localPath);
        }

        using (stream)
        {
            return await UploadFileAsync(stream, storagePath, mimeType);
        }
    }

    public async Task DeleteFileAsync(string storagePath)
    {
        var encodedPath = Uri.EscapeDataString(storagePath);
        var url = $"{AppConfig.FirebaseStorageBaseUrl}/{encodedPath}";

        var token = await SessionService.Instance.GetValidTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _http.SendAsync(request);
        // Ignore 404 — file already deleted
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
            response.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadFileAsync(string downloadUrl)
    {
        var response = await _http.GetAsync(downloadUrl);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    private static string BuildDownloadUrl(string storagePath, string? downloadToken)
    {
        var encodedPath = Uri.EscapeDataString(storagePath);
        var baseUrl = $"{AppConfig.FirebaseStorageBaseUrl}/{encodedPath}?alt=media";
        return downloadToken != null ? $"{baseUrl}&token={downloadToken}" : baseUrl;
    }

    public static string ExtractStoragePathFromUrl(string downloadUrl)
    {
        // Extract path from: .../o/{encodedPath}?alt=media...
        try
        {
            var uri = new Uri(downloadUrl);
            var segments = uri.AbsolutePath.Split("/o/");
            return segments.Length > 1 ? Uri.UnescapeDataString(segments[1]) : string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
