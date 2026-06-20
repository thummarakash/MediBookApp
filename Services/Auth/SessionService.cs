using MediBook.Configuration;
using MediBook.Helpers;
using MediBook.Services.Firebase;

namespace MediBook.Services.Auth;

public class SessionService
{
    public static SessionService Instance { get; } = new();
    private SessionService() { }

    private string? _tok;
    private DateTime _tokExp = DateTime.MinValue;

    public bool IsAuthenticated => SecureStorageHelper.IsLoggedIn();

    public async Task<string?> GetValidTokenAsync()
    {
        if (!string.IsNullOrEmpty(_tok) && DateTime.UtcNow < _tokExp.AddMinutes(-AppConfig.TokenRefreshBufferMinutes))
            return _tok;

        var st_tok = await SecureStorageHelper.GetIdTokenAsync();
        if (string.IsNullOrEmpty(st_tok)) return null;

        bool exp_flag = await SecureStorageHelper.IsTokenExpiredAsync();
        if (!exp_flag)
        {
            _tok = st_tok;
            var expiry = await SecureStorageHelper.GetTokenExpiryAsync();
            _tokExp = expiry ?? DateTime.UtcNow.AddHours(1);
            return _tok;
        }

        // Silent refresh since token is invalid
        return await TryRefreshTokenAsync();
    }

    private async Task<string?> TryRefreshTokenAsync()
    {
        try
        {
            var refresh = await SecureStorageHelper.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refresh)) return null;

            var res = await FirebaseAuthService.Instance.RefreshTokenAsync(refresh);

            var newExpiry = DateTime.UtcNow.AddSeconds(res.ExpiresIn);
            await SecureStorageHelper.UpdateTokenAsync(res.IdToken, newExpiry);
            await SecureStorage.Default.SetAsync(AppConfig.SecureKeys.RefreshToken, res.RefreshToken);

            _tok = res.IdToken;
            _tokExp = newExpiry;
            return _tok;
        }
        catch (Exception refresh_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SessionService] Silent token refresh failed, signing out: {refresh_ex.Message}");
            SignOut();
            return null;
        }
    }

    public async Task SaveSessionAsync(AuthResult res, string role)
    {
        var expiry = res.ExpiresAt;
        await SecureStorageHelper.SaveSessionAsync(
            res.IdToken,
            res.RefreshToken,
            res.UserId,
            res.Email,
            role,
            expiry);

        _tok = res.IdToken;
        _tokExp = expiry;
    }

    public async Task<string?> GetUserIdAsync()
        => await SecureStorageHelper.GetUserIdAsync();

    public async Task<string?> GetUserEmailAsync()
        => await SecureStorageHelper.GetUserEmailAsync();

    public async Task<string?> GetUserRoleAsync()
        => await SecureStorageHelper.GetUserRoleAsync();

    public void SignOut()
    {
        _tok = null;
        _tokExp = DateTime.MinValue;
        SecureStorageHelper.ClearSession();
    }
}
