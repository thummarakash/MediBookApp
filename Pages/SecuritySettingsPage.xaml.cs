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
    }

    private void OnBiometricToggled(object sender, ToggledEventArgs e)
    {
        BiometricService.Instance.SetBiometricsEnabled(e.Value);
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
