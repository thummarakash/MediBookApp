using MediBook.Models;
using MediBook.Services;

namespace MediBook.Pages;

public partial class ProfilePage : ContentPage
{
    private UserAccount? _user;

    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (_user == null)
        {
            await Shell.Current.GoToAsync("//login");
            return;
        }

        HeaderNameLabel.Text = _user.FullName;
        HeaderEmailLabel.Text = _user.Email;
        ProviderLabel.Text = $"{_user.AuthProvider} Account";
        NameEntry.Text = _user.FullName;
        EmailEntry.Text = _user.Email;
        PhoneEntry.Text = _user.Phone;
        DobEntry.Text = _user.DateOfBirth;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (_user == null)
        {
            return;
        }

        _user.FullName = NameEntry.Text ?? string.Empty;
        _user.Phone = PhoneEntry.Text ?? string.Empty;
        _user.DateOfBirth = DobEntry.Text ?? string.Empty;
        await DatabaseService.Instance.UpdateUserAsync(_user);
        await DisplayAlert("Saved", "Profile updated successfully.", "OK");
        HeaderNameLabel.Text = _user.FullName;
    }

    private async void OnSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
    }

    private async void OnContactClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ContactPage));
    }
}
