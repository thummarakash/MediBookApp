using Android.App;
using Firebase.Messaging;
using MediBook.Services.Firebase;

namespace MediBook;

/// <summary>
/// Receives FCM messages on Android.
/// Handles token refresh and both foreground + background notifications.
/// </summary>
[Service(Name = "com.akash.medibook.MediBookFirebaseMessagingService", Exported = false)]
[IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
public class MediBookFirebaseMessagingService : FirebaseMessagingService
{
    public override void OnNewToken(string token)
    {
        base.OnNewToken(token);
        System.Diagnostics.Debug.WriteLine($"[FCM] New token: {token}");
        _ = Task.Run(() => FcmService.Instance.OnTokenRefreshedAsync(token));
    }

    public override void OnMessageReceived(RemoteMessage message)
    {
        base.OnMessageReceived(message);
        var notification = message.GetNotification();
        var title = notification?.Title ?? message.Data.GetValueOrDefault("title", "MediBook");
        var body = notification?.Body ?? message.Data.GetValueOrDefault("body", string.Empty);
        var type = message.Data.GetValueOrDefault("type", "general");

        System.Diagnostics.Debug.WriteLine($"[FCM] Message: {title} — {body}");
        ShowLocalNotification(title, body, type);
    }

    private void ShowLocalNotification(string title, string body, string type)
    {
        var channelId = "medibook_notifications";
        var notificationId = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var intent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? "com.akash.medibook");
        intent?.AddFlags(Android.Content.ActivityFlags.ClearTop | Android.Content.ActivityFlags.SingleTop);

        if (intent != null)
        {
            intent.PutExtra("notification_type", type);
        }

        var pendingIntent = Android.App.PendingIntent.GetActivity(
            this, 0, intent,
            Android.App.PendingIntentFlags.OneShot | Android.App.PendingIntentFlags.Immutable);

        var builder = new Android.App.Notification.Builder(this, channelId)
            .SetContentTitle(title)
            .SetContentText(body)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetAutoCancel(true)
            .SetStyle(new Android.App.Notification.BigTextStyle().BigText(body));

        if (pendingIntent != null)
            builder.SetContentIntent(pendingIntent);

        var notificationManager = (Android.App.NotificationManager?)
            GetSystemService(Android.Content.Context.NotificationService);
        notificationManager?.Notify(notificationId, builder.Build());
    }
}
