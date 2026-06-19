using MediBook.Configuration;
using MediBook.Helpers;
using MediBook.Services.Auth;
using MediBook.Services.Firebase;

namespace MediBook.Services;

public class DocumentService
{
    public static DocumentService Instance { get; } = new();
    private DocumentService() { }

    public event Action<double>? UploadProgressChanged;

    // ── Camera / Gallery ──────────────────────────────────────────────────────

    public async Task<string?> CaptureDocumentPhotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
            return null;

        var photo = await MediaPicker.Default.CapturePhotoAsync();
        return photo == null ? null : await SaveLocallyAsync(photo);
    }

    public async Task<string?> PickDocumentPhotoAsync()
    {
        var options = new MediaPickerOptions { Title = "Select Medical Document" };
        var photo = await MediaPicker.Default.PickPhotoAsync(options);
        return photo == null ? null : await SaveLocallyAsync(photo);
    }

    // ── Firebase Storage upload ────────────────────────────────────────────────

    /// <summary>
    /// Uploads a local file to Firebase Storage and returns the public download URL.
    /// Returns null if the upload fails (non-critical — document is still saved locally).
    /// </summary>
    public async Task<string?> UploadToFirebaseAsync(string localPath, string documentType)
    {
        try
        {
            if (!File.Exists(localPath)) return null;
            if (!ConnectivityHelper.IsConnected) return null;

            var userId = await SessionService.Instance.GetUserIdAsync() ?? "anonymous";
            var folder = documentType == "Prescription"
                ? AppConfig.StoragePaths.Prescriptions
                : AppConfig.StoragePaths.MedicalDocuments;

            var downloadUrl = await FirebaseStorageService.Instance
                .UploadFileFromPathAsync(localPath, userId, folder);

            return downloadUrl;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Storage] Upload failed: {ex.Message}");
            return null;
        }
    }

    // ── Local storage ─────────────────────────────────────────────────────────

    private static async Task<string> SaveLocallyAsync(FileResult file)
    {
        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext)) ext = ".jpg";

        var folder = Path.Combine(FileSystem.AppDataDirectory, "MedicalDocuments");
        Directory.CreateDirectory(folder);

        var localName = $"doc_{DateTime.Now:yyyyMMdd_HHmmss}{ext}";
        var localPath = Path.Combine(folder, localName);

        await using var src = await file.OpenReadAsync();
        await using var dst = File.OpenWrite(localPath);
        await src.CopyToAsync(dst);

        return localPath;
    }
}
