using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class AdminBookingsPage : ContentPage
{
    private readonly AdminBookingsViewModel _vm = new();

    public AdminBookingsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadBookingsCommand.ExecuteAsync(null);
    }
}
