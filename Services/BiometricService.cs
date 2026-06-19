using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace MediBook.Services;

public class BiometricService
{
    private const string BiometricEnabledKey = "medibook_biometric_enabled";
    private const string PinEnabledKey = "medibook_pin_enabled";
    private const string PinValueKey = "medibook_pin_value";

    public static BiometricService Instance { get; } = new();

    private BiometricService() { }

    public bool IsPinEnabled()
    {
        return Preferences.Get(PinEnabledKey, false);
    }

    public void SetPinEnabled(bool enabled)
    {
        Preferences.Set(PinEnabledKey, enabled);
    }

    public string GetSecurityPin()
    {
        return Preferences.Get(PinValueKey, string.Empty);
    }

    public void SetSecurityPin(string pin)
    {
        Preferences.Set(PinValueKey, pin);
    }

    public bool IsBiometricsAvailable()
    {
        // On mobile devices, we assume biometrics are available for demo purposes
        return DeviceInfo.Platform == DevicePlatform.Android || DeviceInfo.Platform == DevicePlatform.iOS;
    }

    public bool IsBiometricsEnabled()
    {
        return Preferences.Get(BiometricEnabledKey, false);
    }

    public void SetBiometricsEnabled(bool enabled)
    {
        Preferences.Set(BiometricEnabledKey, enabled);
    }

    public async Task<bool> AuthenticateAsync(string reason)
    {
        if (!IsBiometricsAvailable())
        {
            await Shell.Current.DisplayAlert("Not Supported", "Biometric authentication is not supported on this device.", "OK");
            return false;
        }

        // Prompt user for authentication using a premium custom platform-native simulation
        string biometricType = DeviceInfo.Platform == DevicePlatform.iOS ? "Face ID / Touch ID" : "Fingerprint / Face Unlock";
        
        bool success = await Shell.Current.DisplayAlert(
            "Biometric Authentication",
            $"{reason}\n\nUse your device's native {biometricType} sensor to authenticate.",
            "Simulate Success",
            "Cancel");

        if (success)
        {
            return true;
        }
        else
        {
            await Shell.Current.DisplayAlert("Authentication Failed", "Biometric verification cancelled or failed. Please try again.", "OK");
            return false;
        }
    }
}
