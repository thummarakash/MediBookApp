using MediBook.Services;

namespace MediBook.Pages;

public partial class DocumentsPage : ContentPage
{
    public DocumentsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        DocumentsCollection.ItemsSource = await DatabaseService.Instance.GetDocumentsForCurrentUserAsync();
    }

    private async void OnUploadClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(UploadDocumentPage));
    }
}
