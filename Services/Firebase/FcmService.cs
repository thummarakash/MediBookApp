using MediBook.Configuration;

namespace MediBook.Services.Firebase;

/// <summary>
/// Manages the FCM device token and persists it to Firestore when the user is authenticated.
/// The actual FCM receiver is in Platforms/Android/MediBookFirebaseMessagingService.cs.
/// </summary>
public class FcmService
{
    public static FcmService Instance { get; } = new();
    private FcmService() { }

    public string? CurrentToken { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            // Read the last known token from preferences
            CurrentToken = Preferences.Default.Get(AppConfig.PrefKeys.FcmToken, string.Empty);
            if (!string.IsNullOrEmpty(CurrentToken))
                await SyncTokenToFirestoreAsync(CurrentToken);
        }
        catch
        {
            // FCM init is non-critical — never crash the app
        }
    }

    public async Task OnTokenRefreshedAsync(string newToken)
    {
        CurrentToken = newToken;
        Preferences.Default.Set(AppConfig.PrefKeys.FcmToken, newToken);
        await SyncTokenToFirestoreAsync(newToken);
    }

    private async Task SyncTokenToFirestoreAsync(string token)
    {
        try
        {
            var userId = await Helpers.SecureStorageHelper.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId)) return;

            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Users,
                userId,
                new Dictionary<string, object> { { "fcmToken", token } }
            );
        }
        catch
        {
            // Non-critical — will retry on next app launch
        }
    }

    public async Task RequestNotificationPermissionAsync()
    {
        // On Android 13+ a runtime permission is required (POST_NOTIFICATIONS)
        // Handled in AndroidManifest + MainActivity via PermissionStatus check
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.PostNotifications>();
    }
}
