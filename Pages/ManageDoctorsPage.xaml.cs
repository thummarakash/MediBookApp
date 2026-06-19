using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class ManageDoctorsPage : ContentPage
{
    private readonly ManageDoctorsViewModel _vm = new();

    public ManageDoctorsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadDataCommand.ExecuteAsync(null);
    }
}
