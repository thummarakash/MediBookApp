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
                FileResult photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo != null)
                {
                    _tempFilePath = photo.FullPath;
                    SelectedFileLabel.Text = $"Selected: {photo.FileName}";
                    DocNameEntry.Text = Path.GetFileNameWithoutExtension(photo.FileName);
                }
            }
            else
            {
                // Fallback / Mock for emulator
                _tempFilePath = "mock_camera_scan.pdf";
                SelectedFileLabel.Text = "Selected: CameraScan.jpg (Simulated)";
                DocNameEntry.Text = "CameraScan";
            }
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Camera Error", $"Error capturing photo: {ex.Message}", "icon_warning.svg");
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
                PickerTitle = "Please select a document or image",
                FileTypes = customFileType
            };

            var file = await FilePicker.Default.PickAsync(options);
            if (file != null)
            {
                var fileInfo = new FileInfo(file.FullPath);
                if (fileInfo.Exists && fileInfo.Length > 10 * 1024 * 1024) // 10MB Limit
                {
                    await ConfirmationPopupPage.ShowAsync(Navigation, "File Too Large", "Maximum allowed file size is 10MB.", "icon_warning.svg");
                    return;
                }

                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext != ".pdf" && ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".docx" && ext != ".txt")
                {
                    await ConfirmationPopupPage.ShowAsync(Navigation, "Invalid Format", "Supported formats: PDF, Images (JPG/PNG), DOCX, TXT.", "icon_warning.svg");
                    return;
                }

                _tempFilePath = file.FullPath;
                SelectedFileLabel.Text = $"Selected: {file.FileName} ({(fileInfo.Length / 1024.0 / 1024.0):F2} MB)";
                DocNameEntry.Text = Path.GetFileNameWithoutExtension(file.FileName);
            }
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Picker Error", $"Error picking file: {ex.Message}", "icon_warning.svg");
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        try
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user == null)
            {
                await ConfirmationPopupPage.ShowAsync(Navigation, "Login Required", "Please login before uploading.", "icon_warning.svg");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            if (string.IsNullOrWhiteSpace(DocNameEntry.Text))
            {
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

            bool confirm = await ConfirmationPopupPage.ShowConfirmAsync(Navigation, "Confirm Upload", "Are you sure you want to upload this medical document?", "Yes", "No");
            if (!confirm) return;

            var document = new MedicalDocument
            {
                UserId = user.Id,
                DocumentType = docType,
                FileName = $"{DocNameEntry.Text.Trim()}{Path.GetExtension(_tempFilePath)}",
                FilePath = string.IsNullOrWhiteSpace(_tempFilePath) ? "simulated_upload.pdf" : _tempFilePath,
                Notes = NotesEditor.Text?.Trim() ?? string.Empty,
                UploadedAt = DateTime.Now
            };

            await DatabaseService.Instance.SaveDocumentAsync(document);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Success", "Document uploaded successfully.");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Upload Error", ex.Message, "icon_warning.svg");
        }
    }

    private void OnDocTypeChanged(object sender, EventArgs e)
    {
        bool isOther = DocTypePicker.SelectedItem?.ToString() == "Other";
        OtherTypePanel.IsVisible = isOther;
        if (!isOther)
            OtherTypeEntry.Text = string.Empty;
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
