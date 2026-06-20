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
        string st_path,
        string mimeType,
        IProgress<double>? progress = null)
    {
        var enc_path = Uri.EscapeDataString(st_path);
        var url = $"{AppConfig.FirebaseStorageBaseUrl}/{enc_path}?uploadType=media";

        var tok = await SessionService.Instance.GetValidTokenAsync();

        using var content = new StreamContent(fileStream);
        content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = content
        };

        if (!string.IsNullOrEmpty(tok))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);

        var res = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        var downTok = doc.TryGetProperty("downloadTokens", out var dt) ? dt.GetString() : null;
        return BuildDownloadUrl(st_path, downTok);
    }

    public async Task<string> UploadFileFromPathAsync(string loc_path, string userId, string folder)
    {
        if (!File.Exists(loc_path))
            throw new FileNotFoundException("Local file not found.", loc_path);

        var mimeType = ImageCompressor.GetMimeType(loc_path);
        var fileName = Path.GetFileName(loc_path);
        var st_path = ImageCompressor.GenerateStoragePath(userId, folder, fileName);

        Stream stream;
        if (mimeType.StartsWith("image/"))
        {
            var compressed = await ImageCompressor.CompressImageAsync(loc_path);
            stream = compressed ?? File.OpenRead(loc_path);
        }
        else
        {
            stream = File.OpenRead(loc_path);
        }

        using (stream)
        {
            return await UploadFileAsync(stream, st_path, mimeType);
        }
    }

    public async Task DeleteFileAsync(string st_path)
    {
        var enc_path = Uri.EscapeDataString(st_path);
        var url = $"{AppConfig.FirebaseStorageBaseUrl}/{enc_path}";

        var tok = await SessionService.Instance.GetValidTokenAsync();
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        if (!string.IsNullOrEmpty(tok))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tok);

        var res = await _http.SendAsync(request);
        
        // Ignore 404 - file is already removed from storage
        if (res.StatusCode != System.Net.HttpStatusCode.NotFound)
            res.EnsureSuccessStatusCode();
    }

    public async Task<byte[]> DownloadFileAsync(string downloadUrl)
    {
        var res = await _http.GetAsync(downloadUrl);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsByteArrayAsync();
    }

    private static string BuildDownloadUrl(string st_path, string? downloadToken)
    {
        var enc_path = Uri.EscapeDataString(st_path);
        var baseUrl = $"{AppConfig.FirebaseStorageBaseUrl}/{enc_path}?alt=media";
        return downloadToken != null ? $"{baseUrl}&token={downloadToken}" : baseUrl;
    }

    public static string ExtractStoragePathFromUrl(string downloadUrl)
    {
        // Try extracting storage path from full download URL
        try
        {
            var u = new Uri(downloadUrl);
            var parts = u.AbsolutePath.Split("/o/");
            return parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
        }
        catch (Exception parse_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StorageService] Url parse failed: {parse_ex.Message}");
            return string.Empty;
        }
    }
}
