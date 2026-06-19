using MediBook.Configuration;
using MediBook.Helpers;
using MediBook.Services.Firebase;

namespace MediBook.Services.Auth;

/// <summary>
/// Single source of truth for the authenticated session.
/// Handles token storage, auto-refresh, and session validation.
/// </summary>
public class SessionService
{
    public static SessionService Instance { get; } = new();
    private SessionService() { }

    private string? _cachedToken;
    private DateTime _cachedTokenExpiry = DateTime.MinValue;

    public bool IsAuthenticated => SecureStorageHelper.IsLoggedIn();

    public async Task<string?> GetValidTokenAsync()
    {
        // Return in-memory cached token if still valid
        if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _cachedTokenExpiry.AddMinutes(-AppConfig.TokenRefreshBufferMinutes))
            return _cachedToken;

        // Load from secure storage
        var storedToken = await SecureStorageHelper.GetIdTokenAsync();
        if (string.IsNullOrEmpty(storedToken)) return null;

        bool isExpired = await SecureStorageHelper.IsTokenExpiredAsync();
        if (!isExpired)
        {
            _cachedToken = storedToken;
            var expiry = await SecureStorageHelper.GetTokenExpiryAsync();
            _cachedTokenExpiry = expiry ?? DateTime.UtcNow.AddHours(1);
            return _cachedToken;
        }

        // Token expired — attempt silent refresh
        return await TryRefreshTokenAsync();
    }

    private async Task<string?> TryRefreshTokenAsync()
    {
        try
        {
            var refreshToken = await SecureStorageHelper.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken)) return null;

            var result = await FirebaseAuthService.Instance.RefreshTokenAsync(refreshToken);

            var newExpiry = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
            await SecureStorageHelper.UpdateTokenAsync(result.IdToken, newExpiry);
            await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.RefreshToken, result.RefreshToken);

            _cachedToken = result.IdToken;
            _cachedTokenExpiry = newExpiry;
            return _cachedToken;
        }
        catch
        {
            // Refresh failed — session is invalid, force logout
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
        _cachedTokenExpiry = expiry;
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
        _cachedTokenExpiry = DateTime.MinValue;
        SecureStorageHelper.ClearSession();
    }
}
