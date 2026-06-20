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

        bool seen_onboard = Preferences.Get("medibook_onboarding_seen", false);
        if (!seen_onboard)
        {
            await Shell.Current.GoToAsync("//onboarding");
            return;
        }

        bool has_session = false;
        try
        {
            var auth_tok = await SessionService.Instance.GetValidTokenAsync();
            has_session = !string.IsNullOrEmpty(auth_tok);
        }
        catch (Exception session_restore_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashPage] Session restore failed: {session_restore_ex.Message}");
            has_session = false;
        }

        if (!has_session)
        {
            has_session = Preferences.Get("medibook_logged_in", false);
        }

        if (has_session)
        {
            bool bio_on = BiometricService.Instance.IsBiometricsEnabled();
            bool pin_on = BiometricService.Instance.IsPinEnabled()
                && !string.IsNullOrEmpty(BiometricService.Instance.GetSecurityPin());

            if (bio_on || pin_on)
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
