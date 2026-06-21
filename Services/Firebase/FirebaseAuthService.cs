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

        var response = await _http.PostAsJsonAsync(url, body);
        var json = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(json);

        if (!response.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(jsonDocument));

        var authResult = ParseAuthResult(jsonDocument.RootElement);
        await UpdateProfileAsync(authResult.IdToken, displayName);
        return authResult;
    }

    public async Task<AuthResult> SignInWithEmailPasswordAsync(string email, string password)
    {
        var body = new { email, password, returnSecureToken = true };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signInWithPassword?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsJsonAsync(url, body);
        var json = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(json);

        if (!response.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(jsonDocument));

        return ParseAuthResult(jsonDocument.RootElement);
    }

    public async Task<AuthResult> RefreshTokenAsync(string refreshToken)
    {
        var body = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };
        var url = $"{AppConfig.FirebaseTokenRefreshUrl}?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsync(url, new FormUrlEncodedContent(body));
        var json = await response.Content.ReadAsStringAsync();
        var jsonDocument = JsonDocument.Parse(json);

        if (!response.IsSuccessStatusCode)
            throw new Exception(ExtractErrorMessage(jsonDocument));

        var root = jsonDocument.RootElement;
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
        var body = new { requestType = "PASSWORD_RESET", email };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:sendOobCode?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            throw new Exception(ExtractErrorMessage(JsonDocument.Parse(json)));
        }
    }

    public async Task ChangePasswordAsync(string idToken, string newPassword)
    {
        var body = new { idToken, password = newPassword, returnSecureToken = true };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:update?key={AppConfig.FirebaseWebApiKey}";

        var response = await _http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            throw new Exception(ExtractErrorMessage(JsonDocument.Parse(json)));
        }
    }

    public async Task DeleteAccountAsync(string idToken)
    {
        var body = new { idToken };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:delete?key={AppConfig.FirebaseWebApiKey}";
        var response = await _http.PostAsJsonAsync(url, body);
        if (!response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            throw new Exception(ExtractErrorMessage(JsonDocument.Parse(json)));
        }
    }

    private async Task UpdateProfileAsync(string idToken, string displayName)
    {
        var body = new { idToken, displayName, returnSecureToken = false };
        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:update?key={AppConfig.FirebaseWebApiKey}";
        await _http.PostAsJsonAsync(url, body);
    }

    private static AuthResult ParseAuthResult(JsonElement element)
    {
        int expiresIn = 3600;
        if (element.TryGetProperty("expiresIn", out var expiryProp))
            int.TryParse(expiryProp.GetString(), out expiresIn);

        return new AuthResult
        {
            IdToken = element.TryGetProperty("idToken", out var token) ? token.GetString() ?? "" : "",
            RefreshToken = element.TryGetProperty("refreshToken", out var refresh) ? refresh.GetString() ?? "" : "",
            UserId = element.TryGetProperty("localId", out var uid) ? uid.GetString() ?? "" : "",
            Email = element.TryGetProperty("email", out var email) ? email.GetString() ?? "" : "",
            DisplayName = element.TryGetProperty("displayName", out var name) ? name.GetString() ?? "" : "",
            ExpiresIn = expiresIn,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn)
        };
    }

    private static string ExtractErrorMessage(JsonDocument jsonDocument)
    {
        try
        {
            var code = jsonDocument.RootElement
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[FirebaseAuthService] Failed to parse error response: {ex.Message}");
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
