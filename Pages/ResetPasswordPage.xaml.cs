using System;
using Microsoft.Maui.Controls;

namespace MediBook.Pages;

[QueryProperty(nameof(Email), "email")]
[QueryProperty(nameof(ValidCode), "validCode")]
public partial class ResetPasswordPage : ContentPage
{
    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set
        {
            _email = Uri.UnescapeDataString(value ?? string.Empty);
            EmailSubtitleLabel.Text = _email;
        }
    }

    public string ValidCode { get; set; } = string.Empty;

    public ResetPasswordPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnResetClicked(object sender, EventArgs e)
    {
        string enteredCode = CodeEntry.Text?.Trim() ?? string.Empty;
        string newPassword = NewPasswordEntry.Text?.Trim() ?? string.Empty;
        string confirmPassword = ConfirmPasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(enteredCode))
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Validation Error", "Please enter the verification code.", "icon_warning.svg", "Try Again");
            return;
        }

        if (enteredCode != ValidCode)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Verification Failed", "The code you entered is incorrect. Please check the simulation code provided.", "icon_warning.svg", "Try Again");
            return;
        }

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Validation Error", "New password must be at least 6 characters.", "icon_warning.svg", "Try Again");
            return;
        }

        if (newPassword != confirmPassword)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Validation Error", "Passwords do not match.", "icon_warning.svg", "Try Again");
            return;
        }

        try
        {
            await Services.DatabaseService.Instance.LoginAsync(Email, newPassword);
            await ConfirmationPopupPage.ShowAsync(
                Navigation,
                "Success",
                "Your password has been successfully reset. You are now logged in.",
                "icon_lock_confirm.svg",
                "Go to Home",
                async () =>
                {
                    await Shell.Current.GoToAsync("//home");
                });
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Reset Failed", ex.Message, "icon_warning.svg", "OK");
        }
    }
}
