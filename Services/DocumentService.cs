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

        var selected_img = await MediaPicker.Default.CapturePhotoAsync();
        return selected_img == null ? null : await SaveLocallyAsync(selected_img);
    }

    public async Task<string?> PickDocumentPhotoAsync()
    {
        var picker_cfg = new MediaPickerOptions { Title = "Select Medical Document" };
        var selected_img = await MediaPicker.Default.PickPhotoAsync(picker_cfg);
        return selected_img == null ? null : await SaveLocallyAsync(selected_img);
    }

    public async Task<string?> UploadToFirebaseAsync(string local_filepath, string category)
    {
        try
        {
            if (!File.Exists(local_filepath)) return null;
            if (!ConnectivityHelper.IsConnected) return null;

            var user_uid = await SessionService.Instance.GetUserIdAsync() ?? "anonymous";
            var target_dir = category == "Prescription"
                ? AppConfig.StoragePaths.Prescriptions
                : AppConfig.StoragePaths.MedicalDocuments;

            // Upload the physical file to Firebase Storage
            var downloadable_url = await FirebaseStorageService.Instance
                .UploadFileFromPathAsync(local_filepath, user_uid, target_dir);

            return downloadable_url;
        }
        catch (Exception upload_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocSvc] Upload to Firebase storage failed: {upload_ex.Message}");
            return null;
        }
    }

    private static async Task<string> SaveLocallyAsync(FileResult file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension)) 
            extension = ".jpg";

        var target_folder = Path.Combine(FileSystem.AppDataDirectory, "MedicalDocuments");
        Directory.CreateDirectory(target_folder);

        var unique_filename = $"doc_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
        var local_filepath = Path.Combine(target_folder, unique_filename);

        await using var read_stream = await file.OpenReadAsync();
        await using var write_stream = File.OpenWrite(local_filepath);
        await read_stream.CopyToAsync(write_stream);

        return local_filepath;
    }
}
