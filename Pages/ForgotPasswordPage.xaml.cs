using System;
using Microsoft.Maui.Controls;

namespace MediBook.Pages;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnSendCodeClicked(object sender, EventArgs e)
    {
        string email = EmailEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        {
            await ConfirmationPopupPage.ShowAsync(
                Navigation,
                "Validation Error",
                "Please enter a valid email address.",
                "icon_warning.svg",
                "Try Again");
            return;
        }

        // Generate a random mock verification code
        Random rand = new Random();
        int code = rand.Next(100000, 999999);

        // Display code in an attractive confirmation pop-up
        await ConfirmationPopupPage.ShowAsync(
            Navigation,
            "Reset Code Sent",
            $"A verification code has been sent to {email}.\n\nSimulation Code: {code}",
            "icon_email.svg",
            "Verify Now",
            async () =>
            {
                // Navigate to Reset Password screen, passing parameters
                await Shell.Current.GoToAsync($"{nameof(ResetPasswordPage)}?email={Uri.EscapeDataString(email)}&validCode={code}");
            });
    }
}
