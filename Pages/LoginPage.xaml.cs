using MediBook.Services;

namespace MediBook.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        bool biometricsEnabled = BiometricService.Instance.IsBiometricsEnabled();
        BiometricBtn.IsVisible = biometricsEnabled;

        if (biometricsEnabled)
        {
            await Task.Delay(500);
            await PerformBiometricLoginAsync();
        }
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await ConfirmationPopupPage.ShowAsync(Navigation, "Missing Details", "Please enter your email and password.", "icon_warning.svg");
                return;
            }

            var user = await DatabaseService.Instance.LoginAsync(EmailEntry.Text, PasswordEntry.Text);
            if (user == null)
            {
                await ConfirmationPopupPage.ShowAsync(Navigation, "Login Failed", "Email or password is incorrect.", "icon_warning.svg");
                return;
            }

            if (user.Role == "Admin")
            {
                await Shell.Current.GoToAsync("//admindashboard");
            }
            else
            {
                await Shell.Current.GoToAsync("//home");
            }
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Error", ex.Message, "icon_warning.svg");
        }
    }

    private async void OnBiometricLoginClicked(object sender, EventArgs e)
    {
        await PerformBiometricLoginAsync();
    }

    private async Task PerformBiometricLoginAsync()
    {
        bool success = await BiometricService.Instance.AuthenticateAsync("Sign in to your MediBook account");
        if (success)
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user == null)
            {
                user = await DatabaseService.Instance.LoginAsync("akash@medibook.com", "password");
            }

            if (user?.Role == "Admin")
            {
                await Shell.Current.GoToAsync("//admindashboard");
            }
            else
            {
                await Shell.Current.GoToAsync("//home");
            }
        }
    }

    private async void OnPatientQuickLoginClicked(object sender, EventArgs e)
    {
        await DatabaseService.Instance.LoginAsync("akash@medibook.com", "password");
        await Shell.Current.GoToAsync("//home");
    }

    private async void OnAdminQuickLoginClicked(object sender, EventArgs e)
    {
        await DatabaseService.Instance.LoginAsync("admin@medibook.com", "password");
        await Shell.Current.GoToAsync("//admindashboard");
    }

    private async void OnGoogleClicked(object sender, EventArgs e)
    {
        try
        {
            await GoogleAuthService.Instance.SignInAsync();
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Google Sign-In", ex.Message, "icon_warning.svg");
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//register");
    }

    private async void OnForgotPasswordClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ForgotPasswordPage));
    }
}
