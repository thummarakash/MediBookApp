using MediBook.Helpers;
using MediBook.Services;
using MediBook.Services.Email;

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
        await AnimationHelper.PageEntranceAsync(this);
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        AccountLabel.Text = user == null
            ? "Not signed in"
            : $"Signed in as {user.FullName} ({user.Email})";
    }

    private async void OnGoogleClicked(object sender, EventArgs e)
    {
        try
        {
            await GoogleAuthService.Instance.SignInAsync();
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            AccountLabel.Text = user == null ? "Signed in" : $"Signed in as {user.FullName} ({user.Email})";
            await ConfirmationPopupPage.ShowAsync(Navigation, "Google Sign-In", "Signed in with Google successfully!");
        }
        catch (OperationCanceledException)
        {
            // User cancelled — no error
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Google Sign-In Failed", ex.Message, "icon_warning.svg");
        }
    }

    private async void OnContactClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ContactPage));

    private async void OnTestEmailClicked(object sender, EventArgs e)
    {
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user == null)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Not Signed In", "Please sign in first.", "icon_warning.svg");
            return;
        }

        var btn = sender as Button;
        if (btn != null) { btn.IsEnabled = false; btn.Text = "Sending..."; }
        try
        {
            bool sent = await SmtpEmailService.Instance.SendWelcomeEmailAsync(user.Email, user.FullName);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Test Email",
                sent ? $"Test email sent to {user.Email}" : "Could not send email. Check internet connection.");
        }
        finally
        {
            if (btn != null) { btn.IsEnabled = true; btn.Text = "Send Test Email"; }
        }
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await ConfirmationPopupPage.ShowConfirmAsync(Navigation,
            "Sign Out", "Are you sure you want to sign out?", "Sign Out", "Cancel");
        if (!confirm) return;

        DatabaseService.Instance.Logout();
        Preferences.Default.Set("medibook_logged_in", false);
        await Shell.Current.GoToAsync("//login");
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//profile");
}
