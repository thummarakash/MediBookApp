using MediBook.Services;

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
            bool biometricsEnabled = BiometricService.Instance.IsBiometricsEnabled();
            bool pinEnabled = BiometricService.Instance.IsPinEnabled() && !string.IsNullOrEmpty(BiometricService.Instance.GetSecurityPin());

            if (biometricsEnabled || pinEnabled)
            {
                // Go directly to PIN Page which will handle PIN keypad and auto-trigger Biometrics if enabled
                await Shell.Current.GoToAsync($"{nameof(PinPage)}?mode=Verify");
            }
            else
            {
                await Shell.Current.GoToAsync("//home");
            }
        }
        else
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
