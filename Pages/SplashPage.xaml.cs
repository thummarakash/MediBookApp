using MediBook.Services;
using MediBook.Services.Auth;

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

        // Try to restore the session from SecureStorage (auto-login with token refresh if needed)
        bool sessionValid = false;
        try
        {
            var token = await SessionService.Instance.GetValidTokenAsync();
            sessionValid = !string.IsNullOrEmpty(token);
        }
        catch
        {
            sessionValid = false;
        }

        if (!sessionValid)
        {
            // Fallback: legacy Preferences-based flag (before Firebase integration)
            sessionValid = Preferences.Get("medibook_logged_in", false);
        }

        if (sessionValid)
        {
            bool biometricsEnabled = BiometricService.Instance.IsBiometricsEnabled();
            bool pinEnabled = BiometricService.Instance.IsPinEnabled()
                && !string.IsNullOrEmpty(BiometricService.Instance.GetSecurityPin());

            if (biometricsEnabled || pinEnabled)
                await Shell.Current.GoToAsync($"{nameof(PinPage)}?mode=Verify");
            else
                await Shell.Current.GoToAsync("//home");
        }
        else
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
