using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;
using Microsoft.Maui.Devices.Sensors;

namespace MediBook.Services;

public class NativeActionService
{
    public static NativeActionService Instance { get; } = new();

    private NativeActionService()
    {
    }

    public void CallClinic(string phoneNumber)
    {
        if (PhoneDialer.Default.IsSupported)
        {
            PhoneDialer.Default.Open(phoneNumber);
        }
    }

    public async Task ComposeEmailAsync(string to, string subject, string body)
    {
        if (!Email.Default.IsComposeSupported)
        {
            return;
        }

        var message = new EmailMessage
        {
            Subject = subject,
            Body = body,
            BodyFormat = EmailBodyFormat.PlainText,
            To = new List<string> { to }
        };
        await Email.Default.ComposeAsync(message);
    }

    public async Task OpenClinicMapAsync()
    {
        var location = new Location(-33.8688, 151.2093);
        var options = new MapLaunchOptions
        {
            Name = "MediBook Medical Centre",
            NavigationMode = NavigationMode.Driving
        };
        await Map.Default.OpenAsync(location, options);
    }
}
