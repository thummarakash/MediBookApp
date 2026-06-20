using System.Net.Http.Json;
using System.Text.Json;
using MediBook.Configuration;
using MediBook.Helpers;

namespace MediBook.Services.Firebase;

public class FirebaseAuthService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(AppConfig.HttpTimeoutSeconds) };

    public static FirebaseAuthService Instance { get; } = new();
    private FirebaseAuthService() { }

    public async Task<AuthResult> SignUpWithEmailPasswordAsync(string email, string password, string displayName)
    {
        var body = new { email, password, returnSecureToken = true };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signUp?key={AppConfig.FirebaseWebApiKey}";

        var res = await _http.PostAsJsonAsync(url, body);
        var json = await res.Content.ReadAsStringAsync();
        var d = JsonDocument.Parse(json);

        if (!res.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(d));

        var elem = d.RootElement;
        var auth_res = ParseAuthResult(elem);

        // Link display name to newly registered account
        await UpdateProfileAsync(auth_res.IdToken, displayName);
        return auth_res;
    }

    public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password)
    {
        var body = new { email, password, returnSecureToken = true };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signInWithPassword?key={AppConfig.FirebaseWebApiKey}";

        var res = await _http.PostAsJsonAsync(url, body);
        var json = await res.Content.ReadAsStringAsync();
        var d = JsonDocument.Parse(json);

        if (!res.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(d));

        return ParseAuthResult(d.RootElement);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var body = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };
        var url = $"{AppConfig.FirebaseTokenRefreshUrl}?key={AppConfig.FirebaseWebApiKey}";

        var res = await _http.PostAsync(url, new FormUrlEncodedContent(body));
        var json = await res.Content.ReadAsStringAsync();
        var d = JsonDocument.Parse(json);

        if (!res.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(d));

        var elem = d.RootElement;
        return new AuthResult
        {
            IdToken = elem.GetProperty("id_token").GetString() ?? "",
            RefreshToken = elem.GetProperty("refresh_token").GetString() ?? "",
            UserId = elem.GetProperty("user_id").GetString() ?? "",
            ExpiresIn = int.Parse(elem.GetProperty("expires_in").GetString() ?? "3600")
        };
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        var body = new { requestType = "PASSWORD_RESET", email };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:sendOobCode?key={AppConfig.FirebaseWebApiKey}";

        var res = await _http.PostAsJsonAsync(url, body);
        if (!res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync();
            var d = JsonDocument.Parse(json);
            throw new Exception(ExtractErrorMessage(d));
        }
    }

    public async Task ChangePasswordAsync(string idToken, string newPassword)
    {
        var body = new { idToken, password = newPassword, returnSecureToken = true };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:update?key={AppConfig.FirebaseWebApiKey}";

        var res = await _http.PostAsJsonAsync(url, body);
        if (!res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync();
            var d = JsonDocument.Parse(json);
            throw new Exception(ExtractErrorMessage(d));
        }
    }

    public async Task DeleteAccountAsync(string idToken)
    {
        var body = new { idToken };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:delete?key={AppConfig.FirebaseWebApiKey}";
        var res = await _http.PostAsJsonAsync(url, body);
        if (!res.IsSuccessStatusCode)
        {
            var json = await res.Content.ReadAsStringAsync();
            throw new Exception(ExtractErrorMessage(JsonDocument.Parse(json)));
        }
    }

    private async Task UpdateProfileAsync(string idToken, string displayName)
    {
        var body = new { idToken, displayName, returnSecureToken = false };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:update?key={AppConfig.FirebaseWebApiKey}";
        await _http.PostAsJsonAsync(url, body);
    }

    private static AuthResult ParseAuthResult(JsonElement elem)
    {
        int expSec = 3600;
        if (elem.TryGetProperty("expiresIn", out var expProp))
            int.TryParse(expProp.GetString(), out expSec);

        return new AuthResult
        {
            IdToken = elem.TryGetProperty("idToken", out var tok) ? tok.GetString() ?? "" : "",
            RefreshToken = elem.TryGetProperty("refreshToken", out var ref_) ? ref_.GetString() ?? "" : "",
            UserId = elem.TryGetProperty("localId", out var uid) ? uid.GetString() ?? "" : "",
            Email = elem.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "",
            DisplayName = elem.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
            ExpiresIn = expSec,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expSec)
        };
    }

    private static string ExtractErrorMessage(JsonDocument d)
    {
        try
        {
            var code = d.RootElement
                .GetProperty("error")
                .GetProperty("message")
                .GetString() ?? "Unknown error";

            return code switch
            {
                "EMAIL_EXISTS" => "This email is already registered. Please sign in or use a different email.",
                "INVALID_EMAIL" => "The email address is not valid.",
                "WEAK_PASSWORD : Password should be at least 6 characters" => "Password must be at least 6 characters.",
                "INVALID_LOGIN_CREDENTIALS" => "Incorrect email or password. Please try again.",
                "EMAIL_NOT_FOUND" => "No account found with this email address.",
                "USER_DISABLED" => "This account has been disabled. Please contact support.",
                "TOO_MANY_ATTEMPTS_TRY_LATER" => "Too many failed attempts. Please try again later.",
                _ => "Authentication failed. Please try again."
            };
        }
        catch (Exception parse_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AuthService] Error parsing Firebase response message: {parse_ex.Message}");
            return "An unexpected error occurred.";
        }
    }
}

public class AuthResult
{
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 3600;
    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(1);
}
