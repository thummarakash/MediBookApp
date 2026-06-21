using MediBook.Configuration;
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
        
        // Reset progress bar and animate to 100% over 1.8s
        if (SplashProgress != null)
        {
            SplashProgress.Progress = 0;
            await SplashProgress.ProgressTo(1.0, 1800, Easing.CubicOut);
        }
        else
        {
            await Task.Delay(2000);
        }

        bool onboardingSeen = Preferences.Get(AppConfig.PrefKeys.OnboardingSeen, false);
        if (!onboardingSeen)
        {
            await Shell.Current.GoToAsync("//onboarding");
            return;
        }

        bool hasSession = false;
        try
        {
            var idToken = await SessionService.Instance.GetValidTokenAsync();
            hasSession = !string.IsNullOrEmpty(idToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashPage] Session restore failed: {ex.Message}");
            hasSession = false;
        }

        if (!hasSession)
            hasSession = Preferences.Get(AppConfig.PrefKeys.LoggedIn, false);

        if (hasSession)
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            string targetRoute = (user != null && user.Role == "Admin") ? "//admindashboard" : "//home";

            bool biometricsOn = BiometricService.Instance.IsBiometricsEnabled();
            bool pinOn = BiometricService.Instance.IsPinEnabled()
                      && !string.IsNullOrEmpty(BiometricService.Instance.GetSecurityPin());

            if (biometricsOn || pinOn)
                await Shell.Current.GoToAsync($"{nameof(PinPage)}?mode=Verify");
            else
                await Shell.Current.GoToAsync(targetRoute);
        }
        else
        {
            await Shell.Current.GoToAsync("//login");
        }
    }
}
