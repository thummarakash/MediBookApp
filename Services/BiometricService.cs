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
            var avail_status = await CrossFingerprint.Current.GetAvailabilityAsync();
            return avail_status == FingerprintAvailability.Available;
        }
        catch (Exception avail_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BiometricService] CrossFingerprint availability check threw an exception: {avail_ex.Message}");
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

            var req_config = new AuthenticationRequestConfiguration(
                "MediBook Authentication",
                reason)
            {
                CancelTitle = "Use PIN Instead",
                FallbackTitle = "Use PIN",
                AllowAlternativeAuthentication = true
            };

            var auth_res = await CrossFingerprint.Current.AuthenticateAsync(req_config);

            if (auth_res.Authenticated)
                return true;

            if (auth_res.Status == FingerprintAuthenticationResultStatus.FallbackRequested
                || auth_res.Status == FingerprintAuthenticationResultStatus.Canceled)
            {
                if (allowPinFallback && IsPinEnabled())
                    return await AuthenticateWithPinFallbackAsync();
            }

            return false;
        }
        catch (Exception auth_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BiometricService] Authentication threw an exception: {auth_ex.Message}");
            
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
        string? pin_input = await Shell.Current.DisplayPromptAsync(
            "Enter PIN",
            "Enter your MediBook security PIN to continue.",
            keyboard: Keyboard.Numeric,
            maxLength: 6);

        if (string.IsNullOrEmpty(pin_input)) return false;
        return pin_input == GetSecurityPin();
    }
}
