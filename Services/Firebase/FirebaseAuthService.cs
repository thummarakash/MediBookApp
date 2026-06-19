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
        var payload = new { email, password, returnSecureToken = true };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signUp?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsJsonAsync(url, payload);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!response.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(doc));

        var root = doc.RootElement;
        var result = ParseAuthResult(root);

        // Update display name after sign-up
        await UpdateProfileAsync(result.IdToken, displayName);

        return result;
    }

    public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password)
    {
        var payload = new { email, password, returnSecureToken = true };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signInWithPassword?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsJsonAsync(url, payload);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!response.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(doc));

        return ParseAuthResult(doc.RootElement);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var payload = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };
        var url = $"{AppConfig.FirebaseTokenRefreshUrl}?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsync(url, new FormUrlEncodedContent(payload));
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!response.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(doc));

        var root = doc.RootElement;
        return new AuthResult
        {
            IdToken = root.GetProperty("id_token").GetString() ?? "",
            RefreshToken = root.GetProperty("refresh_token").GetString() ?? "",
            UserId = root.GetProperty("user_id").GetString() ?? "",
            ExpiresIn = int.Parse(root.GetProperty("expires_in").GetString() ?? "3600")
        };
    }

    public async Task SendPasswordResetEmailAsync(string email)
    {
        var payload = new { requestType = "PASSWORD_RESET", email };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:sendOobCode?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            throw new Exception(ExtractErrorMessage(doc));
        }
    }

    public async Task ChangePasswordAsync(string idToken, string newPassword)
    {
        var payload = new { idToken, password = newPassword, returnSecureToken = true };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:update?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            throw new Exception(ExtractErrorMessage(doc));
        }
    }

    public async Task DeleteAccountAsync(string idToken)
    {
        var payload = new { idToken };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:delete?key={AppConfig.FirebaseWebApiKey}";
        var response = await _http.PostAsJsonAsync(url, payload);
        if (!response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            throw new Exception(ExtractErrorMessage(JsonDocument.Parse(json)));
        }
    }

    private async Task UpdateProfileAsync(string idToken, string displayName)
    {
        var payload = new { idToken, displayName, returnSecureToken = false };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:update?key={AppConfig.FirebaseWebApiKey}";
        await _http.PostAsJsonAsync(url, payload);
    }

    private static AuthResult ParseAuthResult(JsonElement root)
    {
        int expiresIn = 3600;
        if (root.TryGetProperty("expiresIn", out var expProp))
            int.TryParse(expProp.GetString(), out expiresIn);

        return new AuthResult
        {
            IdToken = root.TryGetProperty("idToken", out var tok) ? tok.GetString() ?? "" : "",
            RefreshToken = root.TryGetProperty("refreshToken", out var ref_) ? ref_.GetString() ?? "" : "",
            UserId = root.TryGetProperty("localId", out var uid) ? uid.GetString() ?? "" : "",
            Email = root.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "",
            DisplayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
            ExpiresIn = expiresIn,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn)
        };
    }

    private static string ExtractErrorMessage(JsonDocument doc)
    {
        try
        {
            var code = doc.RootElement
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
        catch
        {
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
