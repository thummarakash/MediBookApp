using MediBook.Configuration;

namespace MediBook.Helpers;

public static class SecureStorageHelper
{
    public static async Task SaveSessionAsync(string idToken, string refreshToken, string userId, string email, string role, DateTime expiry)
    {
        await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.IdToken, idToken);
        await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.RefreshToken, refreshToken);
        await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.UserId, userId);
        await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.UserEmail, email);
        await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.UserRole, role);
        await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.TokenExpiry, expiry.ToString("o"));
        Preferences.Default.Set(AppConfig.PrefKeys.LoggedIn, true);
    }

    public static async Task<string?> GetIdTokenAsync()
        => await SecureStorage.Default.GetAsync(AppConfig.SecureKeys.IdToken);

    public static async Task<string?> GetRefreshTokenAsync()
        => await SecureStorage.Default.GetAsync(AppConfig.SecureKeys.RefreshToken);

    public static async Task<string?> GetUserIdAsync()
        => await SecureStorage.Default.GetAsync(AppConfig.SecureKeys.UserId);

    public static async Task<string?> GetUserEmailAsync()
        => await SecureStorage.Default.GetAsync(AppConfig.SecureKeys.UserEmail);

    public static async Task<string?> GetUserRoleAsync()
        => await SecureStorage.Default.GetAsync(AppConfig.SecureKeys.UserRole);

    public static async Task<DateTime?> GetTokenExpiryAsync()
    {
        var raw = await SecureStorage.Default.GetAsync(AppConfig.SecureKeys.TokenExpiry);
        if (DateTime.TryParse(raw, out var expiry))
            return expiry;
        return null;
    }

    public static async Task<bool> IsTokenExpiredAsync()
    {
        var expiry = await GetTokenExpiryAsync();
        if (expiry == null) return true;
        return DateTime.UtcNow >= expiry.Value.AddMinutes(-AppConfig.TokenRefreshBufferMinutes);
    }

    public static async Task UpdateTokenAsync(string newIdToken, DateTime newExpiry)
    {
        await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.IdToken, newIdToken);
        await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.TokenExpiry, newExpiry.ToString("o"));
    }

    public static void ClearSession()
    {
        SecureStorage.Default.Remove(AppConfig.SecureKeys.IdToken);
        SecureStorage.Default.Remove(AppConfig.SecureKeys.RefreshToken);
        SecureStorage.Default.Remove(AppConfig.SecureKeys.UserId);
        SecureStorage.Default.Remove(AppConfig.SecureKeys.UserEmail);
        SecureStorage.Default.Remove(AppConfig.SecureKeys.UserRole);
        SecureStorage.Default.Remove(AppConfig.SecureKeys.TokenExpiry);
        Preferences.Default.Remove(AppConfig.PrefKeys.LoggedIn);
    }

    public static bool IsLoggedIn()
        => Preferences.Default.Get(AppConfig.PrefKeys.LoggedIn, false);
}
