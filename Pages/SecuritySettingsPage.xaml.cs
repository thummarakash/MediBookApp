using MediBook.Services;

namespace MediBook.Pages;

public partial class SecuritySettingsPage : ContentPage
{
    public SecuritySettingsPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        BiometricSwitch.IsToggled = BiometricService.Instance.IsBiometricsEnabled();
        
        bool pinEnabled = BiometricService.Instance.IsPinEnabled();
        PinSwitch.IsToggled = pinEnabled;
        ChangePinRow.IsVisible = pinEnabled;
        PinDivider.IsVisible = pinEnabled;
    }

    private async void OnBiometricToggled(object sender, ToggledEventArgs e)
    {
        if (e.Value)
        {
            bool authenticated = await BiometricService.Instance.AuthenticateAsync("Confirm fingerprint/face to enable biometric sign-in");
            if (!authenticated)
            {
                BiometricSwitch.Toggled -= OnBiometricToggled;
                BiometricSwitch.IsToggled = false;
                BiometricSwitch.Toggled += OnBiometricToggled;
                return;
            }
        }
        BiometricService.Instance.SetBiometricsEnabled(e.Value);
    }

    private async void OnPinToggled(object sender, ToggledEventArgs e)
    {
        bool pinEnabled = e.Value;
        
        if (pinEnabled && string.IsNullOrEmpty(BiometricService.Instance.GetSecurityPin()))
        {
            // Revert state temporarily so it only remains enabled after a successful setup
            PinSwitch.Toggled -= OnPinToggled;
            PinSwitch.IsToggled = false;
            PinSwitch.Toggled += OnPinToggled;

            await Shell.Current.GoToAsync($"{nameof(PinPage)}?mode=Setup");
        }
        else
        {
            BiometricService.Instance.SetPinEnabled(pinEnabled);
            ChangePinRow.IsVisible = pinEnabled;
            PinDivider.IsVisible = pinEnabled;
        }
    }

    private async void OnChangePinClicked(object sender, EventArgs e)
    {
        string currentPin = BiometricService.Instance.GetSecurityPin();
        if (!string.IsNullOrEmpty(currentPin))
        {
            await Shell.Current.GoToAsync($"{nameof(PinPage)}?mode=VerifyOldPin");
        }
        else
        {
            await Shell.Current.GoToAsync($"{nameof(PinPage)}?mode=Setup");
        }
    }

    private async void OnChangePasswordClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ChangePasswordPage));
    }

    private async void OnPrivacyClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(PrivacyPolicyPage));
    }

    private async void OnTermsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TermsConditionsPage));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
