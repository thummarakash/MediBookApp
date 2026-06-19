using MediBook.Helpers;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.Pages;

public partial class UploadDocumentPage : ContentPage
{
    private string _tempFilePath = string.Empty;

    public UploadDocumentPage()
    {
        InitializeComponent();
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    _tempFilePath = photo.FullPath;
                    SelectedFileLabel.Text = $"Selected: {photo.FileName}";
                    DocNameEntry.Text = Path.GetFileNameWithoutExtension(photo.FileName);
                    await AnimationHelper.SuccessPulseAsync(SelectedFileLabel);
                }
            }
            else
            {
                _tempFilePath = string.Empty;
                SelectedFileLabel.Text = "Camera not supported on this device.";
            }
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Camera Error", ex.Message, "icon_warning.svg");
        }
    }

    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "application/pdf", "image/*", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "text/plain" } },
                { DevicePlatform.iOS, new[] { "com.adobe.pdf", "public.image", "org.openxmlformats.wordprocessingml.document", "public.plain-text" } }
            });

            var options = new PickOptions
            {
                PickerTitle = "Select a medical document or image",
                FileTypes = customFileType
            };

            var file = await FilePicker.Default.PickAsync(options);
            if (file != null)
            {
                var fileInfo = new FileInfo(file.FullPath);
                if (fileInfo.Exists && fileInfo.Length > 10 * 1024 * 1024)
                {
                    await ConfirmationPopupPage.ShowAsync(Navigation, "File Too Large", "Maximum allowed file size is 10 MB.", "icon_warning.svg");
                    return;
                }

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!new[] { ".pdf", ".jpg", ".jpeg", ".png", ".docx", ".txt" }.Contains(ext))
                {
                    await ConfirmationPopupPage.ShowAsync(Navigation, "Invalid Format", "Supported: PDF, JPG, PNG, DOCX, TXT.", "icon_warning.svg");
                    return;
                }

                _tempFilePath = file.FullPath;
                SelectedFileLabel.Text = $"Selected: {file.FileName} ({(fileInfo.Length / 1024.0 / 1024.0):F2} MB)";
                DocNameEntry.Text = Path.GetFileNameWithoutExtension(file.FileName);
                await AnimationHelper.SuccessPulseAsync(SelectedFileLabel);
            }
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Picker Error", ex.Message, "icon_warning.svg");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;

        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user == null)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Login Required", "Please login before uploading.", "icon_warning.svg");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        if (string.IsNullOrWhiteSpace(DocNameEntry.Text))
        {
            await AnimationHelper.ErrorShakeAsync(DocNameEntry);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Validation", "Please enter a document name.", "icon_warning.svg");
            return;
        }

        if (DocTypePicker.SelectedIndex == -1)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Validation", "Please select a document type.", "icon_warning.svg");
            return;
        }

        string docType = DocTypePicker.SelectedItem?.ToString() ?? "Report";
        if (docType == "Other")
        {
            if (string.IsNullOrWhiteSpace(OtherTypeEntry.Text))
            {
                await ConfirmationPopupPage.ShowAsync(Navigation, "Validation", "Please specify the document type.", "icon_warning.svg");
                return;
            }
            docType = OtherTypeEntry.Text.Trim();
        }

        bool confirm = await ConfirmationPopupPage.ShowConfirmAsync(Navigation,
            "Confirm Upload", "Upload this medical document to your records?", "Upload", "Cancel");
        if (!confirm) return;

        if (btn != null) { btn.IsEnabled = false; btn.Text = "Uploading..."; }

        try
        {
            string? storageUrl = null;
            string? storagePath = null;

            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
            {
                // Upload to Firebase Storage in background
                storageUrl = await DocumentService.Instance.UploadToFirebaseAsync(_tempFilePath, docType);
                if (!string.IsNullOrEmpty(storageUrl))
                    storagePath = Helpers.ImageCompressor.GenerateStoragePath(
                        user.FirestoreId.IfEmpty("anonymous"),
                        docType == "Prescription"
                            ? Configuration.AppConfig.StoragePaths.Prescriptions
                            : Configuration.AppConfig.StoragePaths.MedicalDocuments,
                        Path.GetFileName(_tempFilePath));
            }

            var document = new MedicalDocument
            {
                UserFirestoreId = user.FirestoreId,
                UserId = user.Id,
                DocumentType = docType,
                FileName = $"{ValidationHelper.SanitizeInput(DocNameEntry.Text)}{Path.GetExtension(_tempFilePath)}",
                FilePath = _tempFilePath,
                StorageUrl = storageUrl,
                StoragePath = storagePath,
                Notes = NotesEditor.Text?.Trim() ?? string.Empty,
                UploadedAt = DateTime.Now,
                FileSizeBytes = File.Exists(_tempFilePath) ? (int)new FileInfo(_tempFilePath).Length : 0,
                MimeType = ImageCompressor.GetMimeType(_tempFilePath)
            };

            await DatabaseService.Instance.SaveDocumentAsync(document);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Uploaded",
                storageUrl != null
                    ? "Document uploaded to the cloud successfully!"
                    : "Document saved locally. Will sync when online.");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Upload Error", ex.Message, "icon_warning.svg");
        }
        finally
        {
            if (btn != null) { btn.IsEnabled = true; btn.Text = "Upload Document"; }
        }
    }

    private void OnDocTypeChanged(object sender, EventArgs e)
    {
        bool isOther = DocTypePicker.SelectedItem?.ToString() == "Other";
        OtherTypePanel.IsVisible = isOther;
        if (!isOther) OtherTypeEntry.Text = string.Empty;
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}
