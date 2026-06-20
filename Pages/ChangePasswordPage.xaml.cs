using System;
using Microsoft.Maui.Controls;

namespace MediBook.Pages;

public partial class ChangePasswordPage : ContentPage
{
    public ChangePasswordPage()
    {
        InitializeComponent();
    }

    private async void OnUpdatePasswordClicked(object sender, EventArgs e)
    {
        string currentPassword = CurrentPasswordEntry.Text?.Trim() ?? string.Empty;
        string newPassword = NewPasswordEntry.Text?.Trim() ?? string.Empty;
        string confirmPassword = ConfirmPasswordEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Validation Error", "All password fields are required.", "icon_warning.svg");
            return;
        }

        if (newPassword.Length < 6)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Validation Error", "New password must be at least 6 characters long.", "icon_warning.svg");
            return;
        }

        if (newPassword != confirmPassword)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Validation Error", "New password and confirmation password do not match.", "icon_warning.svg");
            return;
        }

        // 1. Verify current password by signing in
        try
        {
            var email = await Services.Auth.SessionService.Instance.GetUserEmailAsync();
            if (string.IsNullOrEmpty(email))
            {
                await ConfirmationPopupPage.ShowAsync(Navigation, "Error", "No active session found. Please log in again.", "icon_warning.svg");
                return;
            }

            // Attempt to authenticate
            var authResult = await Services.Firebase.FirebaseAuthService.Instance.SignInWithEmailPasswordAsync(email, currentPassword);
            
            // 2. Change password
            await Services.Firebase.FirebaseAuthService.Instance.ChangePasswordAsync(authResult.IdToken, newPassword);
            
            // 3. Save new session tokens
            await Services.Auth.SessionService.Instance.SaveSessionAsync(authResult, await Services.Auth.SessionService.Instance.GetUserRoleAsync() ?? "Patient");

            await ConfirmationPopupPage.ShowAsync(Navigation, "Password Updated", "Your password has been changed successfully!");

            // Reset form
            CurrentPasswordEntry.Text = string.Empty;
            NewPasswordEntry.Text = string.Empty;
            ConfirmPasswordEntry.Text = string.Empty;

            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Error", "Current password is incorrect.", "icon_warning.svg");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
