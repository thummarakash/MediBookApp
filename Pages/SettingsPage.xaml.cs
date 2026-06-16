using MediBook.Services;

namespace MediBook.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        AccountLabel.Text = user == null ? "Not signed in" : $"Signed in as {user.FullName} ({user.Email})";
    }

    private async void OnGoogleClicked(object sender, EventArgs e)
    {
        try
        {
            await GoogleAuthService.Instance.SignInAsync();
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            AccountLabel.Text = user == null ? "Signed in" : $"Signed in as {user.FullName} ({user.Email})";
        }
        catch (Exception ex)
        {
            await DisplayAlert("Google", ex.Message, "OK");
        }
    }

    private async void OnContactClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(ContactPage));

    private async void OnTestEmailClicked(object sender, EventArgs e)
    {
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user == null)
        {
            await DisplayAlert("Email", "Please sign in first.", "OK");
            return;
        }

        await NativeActionService.Instance.ComposeEmailAsync(user.Email, "MediBook test email", "This is a MediBook email notification test.");
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        DatabaseService.Instance.Logout();
        await Shell.Current.GoToAsync("//login");
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//profile");
    }
}
