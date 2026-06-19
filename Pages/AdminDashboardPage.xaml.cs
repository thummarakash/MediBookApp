using MediBook.Helpers;
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
        await AnimationHelper.PageEntranceAsync(this);
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnManageClinicsClicked(object sender, EventArgs e)
    {
        if (sender is View v) await AnimationHelper.ButtonPressAsync(v);
        await Shell.Current.GoToAsync(nameof(ManageClinicsPage));
    }

    private async void OnManageDoctorsClicked(object sender, EventArgs e)
    {
        if (sender is View v) await AnimationHelper.ButtonPressAsync(v);
        await Shell.Current.GoToAsync(nameof(ManageDoctorsPage));
    }

    private async void OnViewAllBookingsClicked(object sender, EventArgs e)
    {
        if (sender is View v) await AnimationHelper.ButtonPressAsync(v);
        await Shell.Current.GoToAsync(nameof(AdminBookingsPage));
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await ConfirmationPopupPage.ShowConfirmAsync(Navigation,
            "Sign Out", "Sign out of the admin panel?", "Sign Out", "Cancel");
        if (!confirm) return;
        DatabaseService.Instance.Logout();
        Preferences.Default.Set("medibook_logged_in", false);
        await Shell.Current.GoToAsync("//login");
    }
}
