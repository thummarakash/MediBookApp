using MediBook.Configuration;

namespace MediBook.Services.Firebase;

public class FcmService
{
    public static FcmService Instance { get; } = new();
    private FcmService() { }

    public string? CurrentToken { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            CurrentToken = Preferences.Default.Get(AppConfig.PrefKeys.FcmToken, string.Empty);
            if (!string.IsNullOrEmpty(CurrentToken))
                await SyncTokenToFirestoreAsync(CurrentToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FcmService] Token initialization failed: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FcmService] Token sync to Firestore failed: {ex.Message}");
        }
    }

    public async Task RequestNotificationPermissionAsync()
    {
        var permissionStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (permissionStatus != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.PostNotifications>();
    }
}
