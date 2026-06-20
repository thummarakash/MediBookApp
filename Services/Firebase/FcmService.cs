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
        catch (Exception init_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FcmService] Token initialization failed: {init_ex.Message}");
        }
    }

    public async Task OnTokenRefreshedAsync(string new_tok)
    {
        CurrentToken = new_tok;
        Preferences.Default.Set(AppConfig.PrefKeys.FcmToken, new_tok);
        await SyncTokenToFirestoreAsync(new_tok);
    }

    private async Task SyncTokenToFirestoreAsync(string token)
    {
        try
        {
            var u_id = await Helpers.SecureStorageHelper.GetUserIdAsync();
            if (string.IsNullOrEmpty(u_id)) return;

            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Users,
                u_id,
                new Dictionary<string, object> { { "fcmToken", token } }
            );
        }
        catch (Exception sync_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FcmService] Token synchronization to Firestore failed: {sync_ex.Message}");
        }
    }

    public async Task RequestNotificationPermissionAsync()
    {
        var perm_status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (perm_status != PermissionStatus.Granted)
            await Permissions.RequestAsync<Permissions.PostNotifications>();
    }
}
