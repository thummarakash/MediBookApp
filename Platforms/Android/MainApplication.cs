using Android.App;
using Android.Runtime;
using MediBook.Services.Firebase;

namespace MediBook;

[Application]
public class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
        // Initialize FCM token registration after app is fully ready
        _ = Task.Run(() => FcmService.Instance.InitializeAsync());
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    private void CreateNotificationChannel()
    {
        if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
        {
            var channelId = "medibook_notifications";
            var channelName = "MediBook Notifications";
            var channelDescription = "Appointment reminders and health updates";
            var importance = Android.App.NotificationImportance.High;

            var channel = new Android.App.NotificationChannel(channelId, channelName, importance)
            {
                Description = channelDescription
            };
            channel.EnableVibration(true);
            channel.EnableLights(true);

            var notificationManager = (Android.App.NotificationManager?)
                GetSystemService(Android.Content.Context.NotificationService);
            notificationManager?.CreateNotificationChannel(channel);
        }
    }
}
