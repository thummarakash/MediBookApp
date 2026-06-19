namespace MediBook.Pages;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(2000);

        bool hasSeenOnboarding = Preferences.Get("medibook_onboarding_seen", false);

        if (!hasSeenOnboarding)
        {
            await Shell.Current.GoToAsync("//onboarding");
            return;
        }

        bool isLoggedIn = Preferences.Get("medibook_logged_in", false);

        if (isLoggedIn)
        {
            await Shell.Current.GoToAsync("//home");
        }
        else
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
