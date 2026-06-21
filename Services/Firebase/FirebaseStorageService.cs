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

        var idToken = await SessionService.Instance.GetValidTokenAsync();

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

        if (!string.IsNullOrEmpty(idToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

        var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        var downloadToken = doc.TryGetProperty("downloadTokens", out var dt) ? dt.GetString() : null;
        return BuildDownloadUrl(storagePath, downloadToken);
    }

    public async Task<string> UploadFileFromPathAsync(string localFilePath, string userId, string folder)
    {
        if (!File.Exists(localFilePath))
            throw new FileNotFoundException("Local file not found.", localFilePath);

        var mimeType = ImageCompressor.GetMimeType(localFilePath);
        var fileName = Path.GetFileName(localFilePath);
        var storagePath = ImageCompressor.GenerateStoragePath(userId, folder, fileName);

        Stream stream;
        if (mimeType.StartsWith("image/"))
        {
            var compressed = await ImageCompressor.CompressImageAsync(localFilePath);
            stream = compressed ?? File.OpenRead(localFilePath);
        }
        else
        {
            stream = File.OpenRead(localFilePath);
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

        var idToken = await SessionService.Instance.GetValidTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        if (!string.IsNullOrEmpty(idToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", idToken);

        var response = await _http.SendAsync(request);

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
        try
        {
            var uri = new Uri(downloadUrl);
            var parts = uri.AbsolutePath.Split("/o/");
            return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FirebaseStorageService] Failed to parse storage URL: {ex.Message}");
            return string.Empty;
        }
    }
}
