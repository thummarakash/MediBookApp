using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public class DocumentGroup : List<MedicalDocument>
{
    public string Heading { get; } = string.Empty;
    public DocumentGroup(string heading, List<MedicalDocument> list) : base(list)
    {
        Heading = heading;
    }
}

public partial class DocumentsViewModel : ObservableObject
{
    private List<MedicalDocument> _allDocuments = new();

    [ObservableProperty] ObservableCollection<DocumentGroup> documents = new();
    [ObservableProperty] string selectedCategory = "All";
    [ObservableProperty] string searchText = string.Empty;
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isEmpty;

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allDocuments = await DatabaseService.Instance.GetDocumentsForCurrentUserAsync();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocsVM] load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void SelectCategory(string category)
    {
        SelectedCategory = category;
        ApplyFilters();
    }

    [RelayCommand]
    void Search(string text)
    {
        SearchText = text ?? string.Empty;
        ApplyFilters();
    }

    [RelayCommand]
    async Task ViewDocumentAsync(MedicalDocument doc)
    {
        if (doc == null) return;

        byte[] bytes = Array.Empty<byte>();
        if (doc.StorageUrl != null && doc.StorageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = doc.StorageUrl.Split(',');
            if (parts.Length > 1)
            {
                bytes = Convert.FromBase64String(parts[1]);
            }
        }
        else if (!string.IsNullOrEmpty(doc.FilePath) && File.Exists(doc.FilePath))
        {
            bytes = await File.ReadAllBytesAsync(doc.FilePath);
        }

        if (bytes == null || bytes.Length == 0)
        {
            string fName = string.IsNullOrEmpty(doc.FileName) ? "-" : doc.FileName;
            string docTyp = string.IsNullOrEmpty(doc.DocumentType) ? "-" : doc.DocumentType;
            string uplDate = string.IsNullOrEmpty(doc.UploadedDateText) ? "-" : doc.UploadedDateText;
            string ntes = string.IsNullOrEmpty(doc.Notes) ? "-" : doc.Notes;
            string docmtDetails = $"📄 {fName}\n\nType: {docTyp}\nUploaded: {uplDate}\nNotes: {ntes}";

            await Pages.ConfirmationPopupPage.ShowAsync(
                Shell.Current.CurrentPage.Navigation, 
                "Document Details", 
                docmtDetails, 
                "icon_document.svg"
            );
            return;
        }

        try
        {
            var tempDir = FileSystem.CacheDirectory;
            var tempFilePath = Path.Combine(tempDir, doc.FileName);
            await File.WriteAllBytesAsync(tempFilePath, bytes);

            bool opened = await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                File = new ReadOnlyFile(tempFilePath),
                Title = doc.FileName
            });

            if (!opened)
            {
                string fName = string.IsNullOrEmpty(doc.FileName) ? "-" : doc.FileName;
                string docTyp = string.IsNullOrEmpty(doc.DocumentType) ? "-" : doc.DocumentType;
                string uplDate = string.IsNullOrEmpty(doc.UploadedDateText) ? "-" : doc.UploadedDateText;
                string ntes = string.IsNullOrEmpty(doc.Notes) ? "-" : doc.Notes;
                string docmtDetails = $"📄 {fName}\n\nType: {docTyp}\nUploaded: {uplDate}\nNotes: {ntes}";

                await Pages.ConfirmationPopupPage.ShowAsync(
                    Shell.Current.CurrentPage.Navigation, 
                    "Document Details", 
                    docmtDetails, 
                    "icon_document.svg"
                );
            }
        }
        catch (Exception ex)
        {
            await Pages.ConfirmationPopupPage.ShowAsync(
                Shell.Current.CurrentPage.Navigation, 
                "Error", 
                $"Could not open document: {ex.Message}", 
                "icon_warning.svg"
            );
        }
    }

    [RelayCommand]
    async Task ShareDocumentAsync(MedicalDocument doc)
    {
        if (doc == null) return;
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Share Medical Document",
            Text = $"MediBook Record:\nName: {doc.FileName}\nNotes: {doc.Notes}\nUploaded: {doc.UploadedDateText}",
            Subject = doc.FileName
        });
    }

    [RelayCommand]
    async Task DownloadDocumentAsync(MedicalDocument doc)
    {
        if (doc == null) return;

        byte[] bytes = Array.Empty<byte>();
        if (doc.StorageUrl != null && doc.StorageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var parts = doc.StorageUrl.Split(',');
            if (parts.Length > 1)
            {
                bytes = Convert.FromBase64String(parts[1]);
            }
        }
        else if (!string.IsNullOrEmpty(doc.FilePath) && File.Exists(doc.FilePath))
        {
            bytes = await File.ReadAllBytesAsync(doc.FilePath);
        }

        if (bytes == null || bytes.Length == 0)
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Download Failed", "Document content is empty or not available.");
            return;
        }

        try
        {
#if ANDROID
            var downloadsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads)?.AbsolutePath;
            if (string.IsNullOrEmpty(downloadsPath))
            {
                downloadsPath = Path.Combine(Android.App.Application.Context.GetExternalFilesDir(null)?.AbsolutePath ?? "", "Downloads");
            }
            if (!Directory.Exists(downloadsPath))
            {
                Directory.CreateDirectory(downloadsPath);
            }
            
            var destinationPath = Path.Combine(downloadsPath, doc.FileName);
            
            int count = 1;
            string fileNameOnly = Path.GetFileNameWithoutExtension(doc.FileName);
            string extension = Path.GetExtension(doc.FileName);
            while (File.Exists(destinationPath))
            {
                destinationPath = Path.Combine(downloadsPath, $"{fileNameOnly}({count}){extension}");
                count++;
            }

            await File.WriteAllBytesAsync(destinationPath, bytes);

            try
            {
                var file = new Java.IO.File(destinationPath);
                var mediaScanIntent = new Android.Content.Intent(Android.Content.Intent.ActionMediaScannerScanFile);
                mediaScanIntent.SetData(Android.Net.Uri.FromFile(file));
                Android.App.Application.Context.SendBroadcast(mediaScanIntent);
            }
            catch { }

            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Download Complete", $"Saved to Downloads:\n{Path.GetFileName(destinationPath)}");
#else
            using var stream = new MemoryStream(bytes);
            var fileSaverResult = await CommunityToolkit.Maui.Storage.FileSaver.Default.SaveAsync(doc.FileName, stream, CancellationToken.None);
            if (fileSaverResult.IsSuccessful)
            {
                await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Download Complete", $"Saved successfully to:\n{fileSaverResult.FilePath}");
            }
            else
            {
                await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Download Canceled", "The file was not saved.");
            }
#endif
        }
        catch (Exception ex)
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Error", $"Failed to save document: {ex.Message}");
        }
    }

    [RelayCommand]
    async Task DeleteDocumentAsync(MedicalDocument doc)
    {
        if (doc == null) return;
        bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(Shell.Current.CurrentPage.Navigation, "Delete Record", $"Are you sure you want to delete \"{doc.FileName}\"?", "Delete", "Cancel");
        if (confirm)
        {
            await DatabaseService.Instance.DeleteDocumentAsync(doc.Id);
            await LoadAsync();
        }
    }

    private void ApplyFilters()
    {
        var docs = _allDocuments.AsEnumerable();

        if (!SelectedCategory.Equals("All", StringComparison.OrdinalIgnoreCase))
            docs = docs.Where(d => d.DocumentType.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));

        string q = SearchText.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(q))
            docs = docs.Where(d => d.FileName.Contains(q, StringComparison.OrdinalIgnoreCase)
                                || d.Notes.Contains(q, StringComparison.OrdinalIgnoreCase));

        var result = docs.ToList();

        // group by upload month so the list is easier to browse over time
        var grouped = result
            .OrderByDescending(d => d.UploadedAt)
            .GroupBy(d => d.UploadedAt.ToString("MMMM yyyy"))
            .Select(g => new DocumentGroup(g.Key, g.ToList()))
            .ToList();

        Documents = new ObservableCollection<DocumentGroup>(grouped);
        IsEmpty = result.Count == 0;
    }
}
