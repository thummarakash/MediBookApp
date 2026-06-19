using MediBook.Services;

namespace MediBook.Pages;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(FullNameEntry.Text) || string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await ConfirmationPopupPage.ShowAsync(Navigation, "Missing Details", "Please complete your name, email and password.", "icon_warning.svg");
                return;
            }

            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                await ConfirmationPopupPage.ShowAsync(Navigation, "Password Mismatch", "Password and confirm password do not match.", "icon_warning.svg");
                return;
            }

            await DatabaseService.Instance.RegisterUserAsync(FullNameEntry.Text, EmailEntry.Text, PhoneEntry.Text ?? string.Empty, DobEntry.Text ?? string.Empty, PasswordEntry.Text);
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await ConfirmationPopupPage.ShowAsync(Navigation, "Registration Error", ex.Message, "icon_warning.svg");
        }
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
            await ConfirmationPopupPage.ShowAsync(Navigation, "Google Sign-Up", ex.Message, "icon_warning.svg");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//login");
    }
}
