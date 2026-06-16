using MediBook.Services;

namespace MediBook.Pages;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(EmailEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
            {
                await DisplayAlert("Missing details", "Please enter your email and password.", "OK");
                return;
            }

            var user = await DatabaseService.Instance.LoginAsync(EmailEntry.Text, PasswordEntry.Text);
            if (user == null)
            {
                await DisplayAlert("Login failed", "Email or password is incorrect.", "OK");
                return;
            }

            await Shell.Current.GoToAsync("//home");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
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

    private async void OnRegisterClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//register");
    }
}
