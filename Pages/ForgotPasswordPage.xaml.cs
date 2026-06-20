using MediBook.Helpers;
using MediBook.Services.Firebase;

namespace MediBook.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    private async void OnSendCodeClicked(object sender, EventArgs e)
    {
        var btn = sender as Button;
        if (btn != null) await AnimationHelper.ButtonPressAsync(btn);

        var email = ValidationHelper.SanitizeInput(EmailEntry.Text);

        if (!ValidationHelper.IsValidEmail(email))
        {
            await AnimationHelper.ErrorShakeAsync(EmailEntry.Parent as VisualElement ?? this);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Invalid Email",
                "Please enter a valid email address.", "icon_warning.svg");
            return;
        }

        if (btn != null)
        {
            btn.IsEnabled = false;
            btn.Text = "Sending...";
        }

        try
        {
            // Firebase sends a real password reset email
            await FirebaseAuthService.Instance.SendPasswordResetEmailAsync(email);

            await ConfirmationPopupPage.ShowAsync(
                Navigation,
                "Email Sent",
                $"A password reset link has been sent to {email}.\n\nPlease check your inbox (and spam folder).",
                "icon_email.svg",
                "OK",
                async () => await Shell.Current.GoToAsync(".."));
        }
        catch (Exception reset_ex)
        {
            await AnimationHelper.ErrorShakeAsync(EmailEntry.Parent as VisualElement ?? this);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Error", reset_ex.Message, "icon_warning.svg");
        }
        finally
        {
            if (btn != null)
            {
                btn.IsEnabled = true;
                btn.Text = "Send Reset Link";
            }
        }
    }
}
