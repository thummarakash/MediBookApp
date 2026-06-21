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

    private static CancellationTokenSource? _loadingCts;

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
            btn.Opacity = 0.75;
            _loadingCts?.Cancel();
            _loadingCts = new CancellationTokenSource();
            var token = _loadingCts.Token;

            _ = Task.Run(async () =>
            {
                int dotCount = 0;
                while (!token.IsCancellationRequested)
                {
                    string dots = new string('.', dotCount);
                    string spaces = new string(' ', 3 - dotCount);

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        btn.Text = "Sending" + dots + spaces;
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

        try
        {
            await FirebaseAuthService.Instance.SendPasswordResetEmailAsync(email);

            await ConfirmationPopupPage.ShowAsync(
                Navigation,
                "Email Sent",
                $"A password reset link has been sent to {email}.\n\nPlease check your inbox (and spam folder).",
                "icon_email.svg",
                "OK",
                async () => await Shell.Current.GoToAsync(".."));
        }
        catch (Exception ex)
        {
            await AnimationHelper.ErrorShakeAsync(EmailEntry.Parent as VisualElement ?? this);
            await ConfirmationPopupPage.ShowAsync(Navigation, "Error", ex.Message, "icon_warning.svg");
        }
        finally
        {
            _loadingCts?.Cancel();
            _loadingCts = null;
            if (btn != null)
            {
                btn.IsEnabled = true;
                btn.Opacity = 1.0;
                btn.Text = "Send Reset Link";
            }
        }
    }
}

