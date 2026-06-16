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
                await DisplayAlert("Missing details", "Please complete your name, email and password.", "OK");
                return;
            }

            if (PasswordEntry.Text != ConfirmPasswordEntry.Text)
            {
                await DisplayAlert("Password", "Password and confirm password do not match.", "OK");
                return;
            }

            await DatabaseService.Instance.RegisterUserAsync(FullNameEntry.Text, EmailEntry.Text, PhoneEntry.Text ?? string.Empty, DobEntry.Text ?? string.Empty, PasswordEntry.Text);
            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Registration error", ex.Message, "OK");
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
            await DisplayAlert("Google sign-up", ex.Message, "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//login");
    }
}
