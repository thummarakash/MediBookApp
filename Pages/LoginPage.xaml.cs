using MediBook.Helpers;
using MediBook.Services;
using MediBook.Services.Auth;

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

        // Entrance animation for the form
        var formContent = this.FindByName<ScrollView>("FormScrollView");
        if (formContent != null)
            await AnimationHelper.PageEntranceAsync(formContent, 350);
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn != null) await AnimationHelper.ButtonPressAsync(btn);

        var email = ValidationHelper.SanitizeInput(EmailEntry.Text);
        var password = PasswordEntry.Text;

        var validationError = ValidationHelper.ValidateLogin(email, password);
        if (validationError != null)
        {
            await AnimationHelper.ErrorShakeAsync(EmailEntry.Parent as VisualElement ?? this);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Missing Details", validationError, "icon_warning.svg");
            return;
        }

        await SetLoadingStateAsync(btn, true);
        try
        {
            var user = await DatabaseService.Instance.LoginAsync(email, password!);
            if (user == null)
            {
                await AnimationHelper.ErrorShakeAsync(EmailEntry.Parent as VisualElement ?? this);
                await ConfirmationPopupPage.ShowAsync(Navigation, "Login Failed", "Email or password is incorrect.", "icon_warning.svg");
                return;
            }

            await NavigateAfterLoginAsync(user.Role);
        }
        catch (Exception ex)
        {
            await AnimationHelper.ErrorShakeAsync(EmailEntry.Parent as VisualElement ?? this);
            string friendlyMsg = ex.Message;
            if (ex is System.Net.Http.HttpRequestException || ex.InnerException is System.Net.Http.HttpRequestException || ex is TaskCanceledException)
            {
                friendlyMsg = "Network error. Please check your internet connection and try again.";
            }
            await ConfirmationPopupPage.ShowAsync(Navigation, "Login Failed", friendlyMsg, "icon_warning.svg");
        }
        finally
        {
            await SetLoadingStateAsync(btn, false);
        }
    }

    private async void OnBiometricLoginClicked(object sender, EventArgs e)
        => await PerformBiometricLoginAsync();

    private async Task PerformBiometricLoginAsync()
    {
        bool success = await BiometricService.Instance.AuthenticateAsync("Sign in to your MediBook account");
        if (!success) return;

        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user == null)
        {
            // No stored session — redirect to email login
            await ConfirmationPopupPage.ShowAsync(Navigation, "Session Expired", "Please sign in with your email to continue.", "icon_warning.svg");
            return;
        }

        await NavigateAfterLoginAsync(user.Role);
    }

    private async void OnPatientQuickLoginClicked(object sender, EventArgs e)
    {
        // Quick test login — remove in production
        try
        {
            await DatabaseService.Instance.LoginAsync("patient@medibook.com", "password123");
            await Shell.Current.GoToAsync("//home");
        }
        catch
        {
            // If test account doesn't exist in Firebase, still navigate for UI testing
            await Shell.Current.GoToAsync("//home");
        }
    }

    private async void OnAdminQuickLoginClicked(object sender, EventArgs e)
    {
        try
        {
            await DatabaseService.Instance.LoginAsync("admin@medibook.com", "password123");
            await Shell.Current.GoToAsync("//admindashboard");
        }
        catch
        {
            await Shell.Current.GoToAsync("//admindashboard");
        }
    }

    private async void OnGoogleClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn != null) await AnimationHelper.ButtonPressAsync(btn);

        try
        {
            await GoogleAuthService.Instance.SignInAsync();
            await Shell.Current.GoToAsync("//home");
        }
        catch (OperationCanceledException)
        {
            // User cancelled — no error shown
        }
        catch (Exception ex)
        {
            string msg = ex.Message;
            if (msg.Contains("EMAIL_EXISTS") || msg.Contains("account-exists-with-different-credential"))
            {
                msg = "This email is already registered with a password. Please sign in using your email and password.";
            }
            await ConfirmationPopupPage.ShowAsync(Navigation, "Google Sign-In", msg, "icon_warning.svg");
        }
    }

    private async void OnRegisterClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//register");

    private async void OnForgotPasswordClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ForgotPasswordPage));

    private async Task NavigateAfterLoginAsync(string role)
    {
        PinPage.ResetLockout();
        if (role == "Admin")
            await Shell.Current.GoToAsync("//admindashboard");
        else
            await Shell.Current.GoToAsync("//home");
    }

    private static async Task SetLoadingStateAsync(Button? btn, bool isLoading)
    {
        if (btn == null) return;
        btn.IsEnabled = !isLoading;
        btn.Text = isLoading ? "Signing In..." : "Sign In";
        if (isLoading)
            await AnimationHelper.FadeOutAsync(btn, 100);
        else
            await AnimationHelper.FadeInAsync(btn, 150);
    }

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        TogglePasswordButton.Source = PasswordEntry.IsPassword ? "icon_eye_close.svg" : "icon_eye.svg";
    }
}
