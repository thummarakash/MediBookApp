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
            var availabilityStatus = await CrossFingerprint.Current.GetAvailabilityAsync();
            return availabilityStatus == FingerprintAvailability.Available;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BiometricService] CrossFingerprint availability check failed: {ex.Message}");
            return DeviceInfo.Platform == DevicePlatform.Android
                || DeviceInfo.Platform == DevicePlatform.iOS;
        }
    }

    public bool IsBiometricsAvailable()
        => DeviceInfo.Platform == DevicePlatform.Android
        || DeviceInfo.Platform == DevicePlatform.iOS;

    public async Task<bool> AuthenticateAsync(string reason, bool allowPinFallback = true)
    {
        try
        {
            bool active = await IsBiometricsAvailableAsync();
            if (!active)
            {
                if (allowPinFallback && IsPinEnabled())
                    return await AuthenticateWithPinFallbackAsync();

                if (allowPinFallback)
                {
                    await Shell.Current.DisplayAlert(
                        "Not Available",
                        "Biometric authentication is not available on this device.",
                        "OK");
                }
                return false;
            }

            var authRequest = new AuthenticationRequestConfiguration(
                "MediBook Authentication",
                reason)
            {
                CancelTitle = "Use PIN Instead",
                FallbackTitle = "Use PIN",
                AllowAlternativeAuthentication = true
            };

            var authResult = await CrossFingerprint.Current.AuthenticateAsync(authRequest);

            if (authResult.Authenticated)
                return true;

            if (authResult.Status == FingerprintAuthenticationResultStatus.FallbackRequested
                || authResult.Status == FingerprintAuthenticationResultStatus.Canceled)
            {
                if (allowPinFallback && IsPinEnabled())
                    return await AuthenticateWithPinFallbackAsync();
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BiometricService] Authentication failed: {ex.Message}");

            if (allowPinFallback && IsPinEnabled())
                return await AuthenticateWithPinFallbackAsync();

            if (allowPinFallback)
            {
                bool success = await Shell.Current.DisplayAlert(
                    "Biometric Authentication",
                    reason,
                    "Authenticate",
                    "Cancel");
                return success;
            }
            return false;
        }
    }

    private async Task<bool> AuthenticateWithPinFallbackAsync()
    {
        string? pinInput = await Shell.Current.DisplayPromptAsync(
            "Enter PIN",
            "Enter your MediBook security PIN to continue.",
            keyboard: Keyboard.Numeric,
            maxLength: 6);

        if (string.IsNullOrEmpty(pinInput)) return false;
        return pinInput == GetSecurityPin();
    }
}
