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

    public async Task<string?> CaptureDocumentPhotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
            return null;

        var selectedMedia = await MediaPicker.Default.CapturePhotoAsync();
        return selectedMedia == null ? null : await SaveLocallyAsync(selectedMedia);
    }

    public async Task<string?> PickDocumentPhotoAsync()
    {
        var pickerOptions = new MediaPickerOptions { Title = "Select Medical Document" };
        var selectedMedia = await MediaPicker.Default.PickPhotoAsync(pickerOptions);
        return selectedMedia == null ? null : await SaveLocallyAsync(selectedMedia);
    }

    public async Task<string?> UploadToFirebaseAsync(string localFilePath, string category)
    {
        try
        {
            if (!File.Exists(localFilePath)) return null;
            if (!ConnectivityHelper.IsConnected) return null;

            var userId = await SessionService.Instance.GetUserIdAsync() ?? "anonymous";
            var storageFolder = category == "Prescription"
                ? AppConfig.StoragePaths.Prescriptions
                : AppConfig.StoragePaths.MedicalDocuments;

            var downloadUrl = await FirebaseStorageService.Instance
                .UploadFileFromPathAsync(localFilePath, userId, storageFolder);

            return downloadUrl;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentService] Upload to Firebase storage failed: {ex.Message}");
            return null;
        }
    }

    private static async Task<string> SaveLocallyAsync(FileResult file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
            extension = ".jpg";

        var targetFolder = Path.Combine(FileSystem.AppDataDirectory, "MedicalDocuments");
        Directory.CreateDirectory(targetFolder);

        var uniqueFileName = $"doc_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
        var localFilePath = Path.Combine(targetFolder, uniqueFileName);

        await using var readStream = await file.OpenReadAsync();
        await using var writeStream = File.OpenWrite(localFilePath);
        await readStream.CopyToAsync(writeStream);

        return localFilePath;
    }
}
