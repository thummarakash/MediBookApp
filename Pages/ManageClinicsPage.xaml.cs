using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class ManageClinicsPage : ContentPage
{
    private readonly ManageClinicsViewModel _vm = new();

    public ManageClinicsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadClinicsCommand.ExecuteAsync(null);
    }
}
