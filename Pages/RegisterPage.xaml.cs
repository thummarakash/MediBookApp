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
        var dob = ValidationHelper.SanitizeInput(DobEntry.Text);

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

            // Send welcome email in the background
            _ = Task.Run(() => SmtpEmailService.Instance.SendWelcomeEmailAsync(email, fullName));

            await AnimationHelper.SuccessPulseAsync(btn as VisualElement ?? this);
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await AnimationHelper.ErrorShakeAsync(EmailEntry.Parent as VisualElement ?? this);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Registration Failed", ex.Message, "icon_warning.svg");
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
            // User cancelled
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Google Sign-Up", ex.Message, "icon_warning.svg");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("//login");

    private static async Task SetLoadingStateAsync(Button? btn, bool isLoading)
    {
        if (btn == null) return;
        btn.IsEnabled = !isLoading;
        btn.Text = isLoading ? "Creating Account..." : "Create Account";
    }
}
