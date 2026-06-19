using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class DocumentsPage : ContentPage
{
    private readonly DocumentsViewModel _vm = new();

    public DocumentsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchCommand.Execute(e.NewTextValue);
    }

    private void OnChipTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is string category)
        {
            _vm.SelectCategoryCommand.Execute(category);
            UpdateChipUI(category);
        }
    }

    private void UpdateChipUI(string active)
    {
        var activeBlue = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#185FA5"));
        var lightBlue = Color.FromArgb("#E6F1FB");

        AllChip.BackgroundColor = active == "All" ? activeBlue : lightBlue;
        AllChipText.TextColor = active == "All" ? Colors.White : activeBlue;
        AllChipText.FontAttributes = active == "All" ? FontAttributes.Bold : FontAttributes.None;

        PrescriptionChip.BackgroundColor = active == "Prescription" ? activeBlue : lightBlue;
        PrescriptionChipText.TextColor = active == "Prescription" ? Colors.White : activeBlue;
        PrescriptionChipText.FontAttributes = active == "Prescription" ? FontAttributes.Bold : FontAttributes.None;

        BloodChip.BackgroundColor = active == "Blood Test" ? activeBlue : lightBlue;
        BloodChipText.TextColor = active == "Blood Test" ? Colors.White : activeBlue;
        BloodChipText.FontAttributes = active == "Blood Test" ? FontAttributes.Bold : FontAttributes.None;

        ReportChip.BackgroundColor = active == "Report" ? activeBlue : lightBlue;
        ReportChipText.TextColor = active == "Report" ? Colors.White : activeBlue;
        ReportChipText.FontAttributes = active == "Report" ? FontAttributes.Bold : FontAttributes.None;
    }

    private async void OnUploadClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(UploadDocumentPage));
    }

    private async void OnOptionsClicked(object sender, EventArgs e)
    {
        if (sender is ImageButton button && button.CommandParameter is Models.MedicalDocument doc)
        {
            string action = await DisplayActionSheet(
                "Document Actions", 
                "Cancel", 
                null, 
                "View Details", 
                "Share Record", 
                "Download File", 
                "Delete Document");

            switch (action)
            {
                case "View Details":
                    _vm.ViewDocumentCommand.Execute(doc);
                    break;
                case "Share Record":
                    _vm.ShareDocumentCommand.Execute(doc);
                    break;
                case "Download File":
                    _vm.DownloadDocumentCommand.Execute(doc);
                    break;
                case "Delete Document":
                    _vm.DeleteDocumentCommand.Execute(doc);
                    break;
            }
        }
    }
    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//profile");
    }
}

