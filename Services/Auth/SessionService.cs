using MediBook.Configuration;
using MediBook.Helpers;
using MediBook.Services.Firebase;

namespace MediBook.Services.Auth;

public class SessionService
{
    public static SessionService Instance { get; } = new();
    private SessionService() { }

    private string? _cachedToken;
    private DateTime _tokenExpiry = DateTime.MinValue;

    public bool IsAuthenticated => SecureStorageHelper.IsLoggedIn();

    public async Task<string?> GetValidTokenAsync()
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiry.AddMinutes(-AppConfig.TokenRefreshBufferMinutes))
            return _cachedToken;

        var storedToken = await SecureStorageHelper.GetIdTokenAsync();
        if (string.IsNullOrEmpty(storedToken)) return null;

        bool isExpired = await SecureStorageHelper.IsTokenExpiredAsync();
        if (!isExpired)
        {
            _cachedToken = storedToken;
            var expiry = await SecureStorageHelper.GetTokenExpiryAsync();
            _tokenExpiry = expiry ?? DateTime.UtcNow.AddHours(1);
            return _cachedToken;
        }

        return await TryRefreshTokenAsync();
    }

    private async Task<string?> TryRefreshTokenAsync()
    {
        try
        {
            var refreshToken = await SecureStorageHelper.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken)) return null;

            var authResult = await FirebaseAuthService.Instance.RefreshTokenAsync(refreshToken);

            var newExpiry = DateTime.UtcNow.AddSeconds(authResult.ExpiresIn);
            await SecureStorageHelper.UpdateTokenAsync(authResult.IdToken, newExpiry);
            await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.RefreshToken, authResult.RefreshToken);

            _cachedToken = authResult.IdToken;
            _tokenExpiry = newExpiry;
            return _cachedToken;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SessionService] Silent token refresh failed: {ex.Message}");
            SignOut();
            return null;
        }
    }

    public async Task SaveSessionAsync(AuthResult authResult, string role)
    {
        var expiry = authResult.ExpiresAt;
        await SecureStorageHelper.SaveSessionAsync(
            authResult.IdToken,
            authResult.RefreshToken,
            authResult.UserId,
            authResult.Email,
            role,
            expiry);

        _cachedToken = authResult.IdToken;
        _tokenExpiry = expiry;
    }

    public async Task<string?> GetUserIdAsync()
        => await SecureStorageHelper.GetUserIdAsync();

    public async Task<string?> GetUserEmailAsync()
        => await SecureStorageHelper.GetUserEmailAsync();

    public async Task<string?> GetUserRoleAsync()
        => await SecureStorageHelper.GetUserRoleAsync();

    public void SignOut()
    {
        _cachedToken = null;
        _tokenExpiry = DateTime.MinValue;
        SecureStorageHelper.ClearSession();
    }
}
