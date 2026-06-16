using MediBook.Models;
using MediBook.Services;

namespace MediBook.Pages;

public partial class UploadDocumentPage : ContentPage
{
    private string? _selectedPath;

    public UploadDocumentPage()
    {
        InitializeComponent();
        DocumentTypePicker.SelectedIndex = 0;
    }

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            _selectedPath = await DocumentService.Instance.CaptureDocumentPhotoAsync();
            ShowSelectedFile();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Camera", ex.Message, "OK");
        }
    }

    private async void OnPickPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            _selectedPath = await DocumentService.Instance.PickDocumentPhotoAsync();
            ShowSelectedFile();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Gallery", ex.Message, "OK");
        }
    }

    private void ShowSelectedFile()
    {
        if (string.IsNullOrWhiteSpace(_selectedPath))
        {
            SelectedFileLabel.Text = "No file selected";
            PreviewImage.Source = "icon_camera.svg";
            return;
        }

        SelectedFileLabel.Text = Path.GetFileName(_selectedPath);
        PreviewImage.Source = ImageSource.FromFile(_selectedPath);
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user == null)
        {
            await DisplayAlert("Login required", "Please login first.", "OK");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        if (string.IsNullOrWhiteSpace(_selectedPath))
        {
            await DisplayAlert("Document", "Please take or select a photo first.", "OK");
            return;
        }

        var document = new MedicalDocument
        {
            UserId = user.Id,
            DocumentType = DocumentTypePicker.SelectedItem?.ToString() ?? "Medical Document",
            FileName = Path.GetFileName(_selectedPath),
            FilePath = _selectedPath,
            Notes = NotesEditor.Text ?? string.Empty,
            UploadedAt = DateTime.Now
        };

        await DatabaseService.Instance.SaveDocumentAsync(document);
        await DisplayAlert("Saved", "Document saved to the MediBook local database.", "OK");
        await Shell.Current.GoToAsync("//documents");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//documents");
    }
}
