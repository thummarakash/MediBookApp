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
    private List<MedicalDocument> _all = new();

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
            _all = await DatabaseService.Instance.GetDocumentsForCurrentUserAsync();
            ApplyFilters();
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
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Document Details", $"📄 {doc.FileName}\n\nType: {doc.DocumentType}\nUploaded: {doc.UploadedDateText}\nNotes: {doc.Notes}");
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
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Download Complete", $"\"{doc.FileName}\" has been saved to your device.");
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
        var docs = _all.AsEnumerable();

        if (!SelectedCategory.Equals("All", StringComparison.OrdinalIgnoreCase))
            docs = docs.Where(d => d.DocumentType.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase));

        var query = SearchText.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(query))
            docs = docs.Where(d => d.FileName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                                   d.Notes.Contains(query, StringComparison.OrdinalIgnoreCase));

        var result = docs.ToList();

        var grouped = result
            .OrderByDescending(d => d.UploadedAt)
            .GroupBy(d => d.UploadedAt.ToString("MMMM yyyy"))
            .Select(g => new DocumentGroup(g.Key, g.ToList()))
            .ToList();

        Documents = new ObservableCollection<DocumentGroup>(grouped);
        IsEmpty = result.Count == 0;
    }
}
