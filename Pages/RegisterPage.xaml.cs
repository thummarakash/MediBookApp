using MediBook.Helpers;
using MediBook.Services;
using MediBook.Services.Email;

namespace MediBook.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var form = this.FindByName<ScrollView>("FormScrollView");
        if (form != null) await AnimationHelper.PageEntranceAsync(form, 350);
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn != null) await AnimationHelper.ButtonPressAsync(btn);

        var fullName = ValidationHelper.SanitizeInput(FullNameEntry.Text);
        var email = ValidationHelper.SanitizeInput(EmailEntry.Text);
        var password = PasswordEntry.Text ?? string.Empty;
        var confirmPassword = ConfirmPasswordEntry.Text ?? string.Empty;
        var phone = ValidationHelper.SanitizeInput(PhoneEntry.Text);
        var dob = DobDatePicker.Date?.ToString("dd/MM/yyyy") ?? "";

        var validationError = ValidationHelper.ValidateRegistration(fullName, email, password, confirmPassword);
        if (validationError != null)
        {
            await AnimationHelper.ErrorShakeAsync(FullNameEntry.Parent as VisualElement ?? this);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Registration Error", validationError, "icon_warning.svg");
            return;
        }

        if (!string.IsNullOrEmpty(phone) && !ValidationHelper.IsValidPhone(phone))
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Invalid Phone", "Please enter a valid phone number.", "icon_warning.svg");
            return;
        }

        await SetLoadingStateAsync(btn, true);
        try
        {
            var user = await DatabaseService.Instance.RegisterUserAsync(fullName, email, phone, dob, password);

            _ = Task.Run(() => SmtpEmailService.Instance.SendWelcomeEmailAsync(email, fullName));

            await AnimationHelper.SuccessPulseAsync(btn as VisualElement ?? this);
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await AnimationHelper.ErrorShakeAsync(EmailEntry.Parent as VisualElement ?? this);
            string friendlyMsg = ex.Message;
            if (ex is System.Net.Http.HttpRequestException || ex.InnerException is System.Net.Http.HttpRequestException || ex is TaskCanceledException)
                friendlyMsg = "Network error. Please check your internet connection and try again.";
            await ConfirmationPopupPage.ShowAsync(Navigation, "Registration Failed", friendlyMsg, "icon_warning.svg");
        }
        finally
        {
            await SetLoadingStateAsync(btn, false);
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
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Google Sign-Up", ex.Message, "icon_warning.svg");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//login");

    private static CancellationTokenSource? _loadingCts;

    private static async Task SetLoadingStateAsync(Button? btn, bool isLoading)
    {
        if (btn == null) return;
        btn.IsEnabled = !isLoading;
        btn.Opacity = isLoading ? 0.75 : 1.0;

        if (isLoading)
        {
            _loadingCts?.Cancel();
            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            // Start dynamic dot animation loop
            _ = Task.Run(async () =>
            {
                int dotCount = 0;
                while (!token.IsCancellationRequested)
                {
                    string dots = new string('.', dotCount);
                    string spaces = new string(' ', 3 - dotCount);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        btn.Text = "Creating Account" + dots + spaces;
                    });

                    dotCount = (dotCount + 1) % 4;

                    try
                    {
                        await Task.Delay(400, token);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }
        else
        {
            _loadingCts?.Cancel();
            _loadingCts = null;
            btn.Text = "Create Account";
        }
        await Task.CompletedTask;
    }

    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        TogglePasswordButton.Source = PasswordEntry.IsPassword ? "icon_eye_close.svg" : "icon_eye.svg";
    }

    private void OnToggleConfirmPasswordClicked(object sender, EventArgs e)
    {
        ConfirmPasswordEntry.IsPassword = !ConfirmPasswordEntry.IsPassword;
        ToggleConfirmPasswordButton.Source = ConfirmPasswordEntry.IsPassword ? "icon_eye_close.svg" : "icon_eye.svg";
    }
}
