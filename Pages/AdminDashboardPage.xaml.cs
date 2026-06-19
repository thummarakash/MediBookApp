using MediBook.Services;
using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class AdminDashboardPage : ContentPage
{
    private readonly AdminDashboardViewModel _vm = new();

    public AdminDashboardPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnManageClinicsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ManageClinicsPage));

    private async void OnManageDoctorsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ManageDoctorsPage));

    private async void OnViewAllBookingsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(AdminBookingsPage));

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        DatabaseService.Instance.Logout();
        await Shell.Current.GoToAsync("//login");
    }
}
