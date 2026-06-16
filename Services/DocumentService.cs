using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace MediBook.Services;

public class DocumentService
{
    public static DocumentService Instance { get; } = new();

    private DocumentService()
    {
    }

    public async Task<string?> CaptureDocumentPhotoAsync()
    {
        if (!MediaPicker.Default.IsCaptureSupported)
        {
            return null;
        }

        var photo = await MediaPicker.Default.CapturePhotoAsync();
        return photo == null ? null : await CopyFileToAppDataAsync(photo);
    }

    public async Task<string?> PickDocumentPhotoAsync()
    {
        var photo = await MediaPicker.Default.PickPhotoAsync();
        return photo == null ? null : await CopyFileToAppDataAsync(photo);
    }

    private static async Task<string> CopyFileToAppDataAsync(FileResult file)
    {
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        var documentsFolder = Path.Combine(FileSystem.AppDataDirectory, "MedicalDocuments");
        Directory.CreateDirectory(documentsFolder);
        var localFileName = $"doc_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
        var localPath = Path.Combine(documentsFolder, localFileName);

        await using var sourceStream = await file.OpenReadAsync();
        await using var targetStream = File.OpenWrite(localPath);
        await sourceStream.CopyToAsync(targetStream);
        return localPath;
    }
}
