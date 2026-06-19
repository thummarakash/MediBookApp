using MediBook.Services;
using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class ProfilePage : ContentPage
{
    private readonly ProfileViewModel _vm = new();

    public ProfilePage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user != null)
        {
            CustomTabBarControl.IsAdmin = user.Role == "Admin";
        }
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private async void OnNotificationsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NotificationSettingsPage));
    }

    private async void OnSecurityClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SecuritySettingsPage));
    }

    private async void OnSignOutClicked(object sender, EventArgs e)
    {
        bool confirm = await ConfirmationPopupPage.ShowConfirmAsync(Navigation, "Sign Out", "Are you sure you want to sign out?", "Yes", "No");
        if (confirm)
        {
            DatabaseService.Instance.Logout();
            await Shell.Current.GoToAsync("//login");
        }

    }

    private async void OnMedicalRecordsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//documents");
    }
}


