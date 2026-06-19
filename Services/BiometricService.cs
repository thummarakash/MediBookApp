using MediBook.Configuration;
using Plugin.Fingerprint;
using Plugin.Fingerprint.Abstractions;

namespace MediBook.Services;

public class BiometricService
{
    public static BiometricService Instance { get; } = new();
    private BiometricService() { }

    public bool IsPinEnabled()
        => Preferences.Default.Get(AppConfig.PrefKeys.PinEnabled, false);

    public void SetPinEnabled(bool enabled)
        => Preferences.Default.Set(AppConfig.PrefKeys.PinEnabled, enabled);

    public string GetSecurityPin()
        => Preferences.Default.Get(AppConfig.PrefKeys.PinValue, string.Empty);

    public void SetSecurityPin(string pin)
        => Preferences.Default.Set(AppConfig.PrefKeys.PinValue, pin);

    public bool IsBiometricsEnabled()
        => Preferences.Default.Get(AppConfig.PrefKeys.BiometricEnabled, false);

    public void SetBiometricsEnabled(bool enabled)
        => Preferences.Default.Set(AppConfig.PrefKeys.BiometricEnabled, enabled);

    public async Task<bool> IsBiometricsAvailableAsync()
    {
        try
        {
            var availability = await CrossFingerprint.Current.GetAvailabilityAsync();
            return availability == FingerprintAvailability.Available;
        }
        catch
        {
            return DeviceInfo.Platform == DevicePlatform.Android
                || DeviceInfo.Platform == DevicePlatform.iOS;
        }
    }

    // Kept for backward compatibility with SplashPage/SecuritySettingsPage
    public bool IsBiometricsAvailable()
        => DeviceInfo.Platform == DevicePlatform.Android
        || DeviceInfo.Platform == DevicePlatform.iOS;

    public async Task<bool> AuthenticateAsync(string reason)
    {
        try
        {
            bool available = await IsBiometricsAvailableAsync();
            if (!available)
            {
                // Fall back to PIN if biometrics not available
                if (IsPinEnabled())
                    return await AuthenticateWithPinFallbackAsync();

                await Shell.Current.DisplayAlert(
                    "Not Available",
                    "Biometric authentication is not available on this device.",
                    "OK");
                return false;
            }

            var config = new AuthenticationRequestConfiguration(
                "MediBook Authentication",
                reason)
            {
                CancelTitle = "Use PIN Instead",
                FallbackTitle = "Use PIN",
                AllowAlternativeAuthentication = true
            };

            var result = await CrossFingerprint.Current.AuthenticateAsync(config);

            if (result.Authenticated)
                return true;

            if (result.Status == FingerprintAuthenticationResultStatus.FallbackRequested
                || result.Status == FingerprintAuthenticationResultStatus.Canceled)
            {
                if (IsPinEnabled())
                    return await AuthenticateWithPinFallbackAsync();
            }

            return false;
        }
        catch
        {
            // If Plugin.Fingerprint fails, fall back to PIN or simple prompt
            if (IsPinEnabled())
                return await AuthenticateWithPinFallbackAsync();

            bool success = await Shell.Current.DisplayAlert(
                "Biometric Authentication",
                reason,
                "Authenticate",
                "Cancel");
            return success;
        }
    }

    private async Task<bool> AuthenticateWithPinFallbackAsync()
    {
        string? enteredPin = await Shell.Current.DisplayPromptAsync(
            "Enter PIN",
            "Enter your MediBook security PIN to continue.",
            keyboard: Keyboard.Numeric,
            maxLength: 6);

        if (string.IsNullOrEmpty(enteredPin)) return false;
        return enteredPin == GetSecurityPin();
    }
}
