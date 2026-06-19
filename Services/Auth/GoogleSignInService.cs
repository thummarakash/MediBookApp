using System.Net.Http.Json;
using System.Text.Json;
using MediBook.Configuration;

namespace MediBook.Services.Auth;

/// <summary>
/// Implements Google Sign-In via MAUI WebAuthenticator → OAuth 2.0 → Firebase identity.
/// The user is redirected to Google's consent screen in a Chrome Custom Tab,
/// then the ID token is exchanged with Firebase Auth REST API.
/// </summary>
public class GoogleSignInService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(AppConfig.HttpTimeoutSeconds) };

    public static GoogleSignInService Instance { get; } = new();
    private GoogleSignInService() { }

    public async Task<GoogleSignInResult> SignInAsync()
    {
        // Build the Google OAuth URL
        var nonce = Guid.NewGuid().ToString("N");
        var redirectUri = "com.akash.medibook://oauth2redirect";

        var authUrl = new Uri(
            $"https://accounts.google.com/o/oauth2/auth"
            + $"?client_id={Uri.EscapeDataString(AppConfig.GoogleWebClientId)}"
            + $"&redirect_uri={Uri.EscapeDataString(redirectUri)}"
            + $"&response_type=code"
            + $"&scope={Uri.EscapeDataString("openid email profile")}"
            + $"&nonce={nonce}"
            + $"&prompt=select_account"
        );

        var callbackUri = new Uri(redirectUri);

        WebAuthenticatorResult webResult;
        try
        {
            webResult = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUri);
        }
        catch (TaskCanceledException)
        {
            throw new OperationCanceledException("Google Sign-In was cancelled.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Google Sign-In failed: {ex.Message}");
        }

        // Exchange authorization code for ID token via Google token endpoint
        webResult.Properties.TryGetValue("code", out var authCode);
        if (string.IsNullOrEmpty(authCode))
        {
            // Some flows return id_token directly
            webResult.Properties.TryGetValue("id_token", out var directIdToken);
            if (!string.IsNullOrEmpty(directIdToken))
                return await SignInWithGoogleIdTokenAsync(directIdToken);

            throw new Exception("Google did not return an authorization code. Please try again.");
        }

        var tokenResponse = await ExchangeCodeForTokenAsync(authCode, redirectUri);
        return await SignInWithGoogleIdTokenAsync(tokenResponse.IdToken);
    }

    private async Task<GoogleSignInResult> SignInWithGoogleIdTokenAsync(string googleIdToken)
    {
        var payload = new
        {
            postBody = $"id_token={googleIdToken}&providerId=google.com",
            requestUri = "http://localhost",
            returnIdpCredential = true,
            returnSecureToken = true
        };

        var url = $"{AppConfig.FirebaseAuthBaseUrl}/accounts:signInWithIdp?key={AppConfig.FirebaseWebApiKey}";
        var response = await _http.PostAsJsonAsync(url, payload);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        if (!response.IsSuccessStatusCode)
        {
            var errorMsg = doc.RootElement
                .TryGetProperty("error", out var err)
                ? err.GetProperty("message").GetString() ?? "Sign-in failed"
                : "Google Sign-In failed";
            throw new Exception(errorMsg);
        }

        var root = doc.RootElement;
        return new GoogleSignInResult
        {
            IdToken = root.TryGetProperty("idToken", out var tok) ? tok.GetString() ?? "" : "",
            RefreshToken = root.TryGetProperty("refreshToken", out var rt) ? rt.GetString() ?? "" : "",
            UserId = root.TryGetProperty("localId", out var uid) ? uid.GetString() ?? "" : "",
            Email = root.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "",
            DisplayName = root.TryGetProperty("displayName", out var dn) ? dn.GetString() ?? "" : "",
            PhotoUrl = root.TryGetProperty("photoUrl", out var ph) ? ph.GetString() ?? "" : "",
            ExpiresIn = int.TryParse(
                root.TryGetProperty("expiresIn", out var exp) ? exp.GetString() : "3600", out var expVal)
                ? expVal : 3600,
            IsNewUser = root.TryGetProperty("isNewUser", out var nu) && nu.GetBoolean()
        };
    }

    private async Task<(string IdToken, string AccessToken)> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        // NOTE: For a production app the client_secret exchange MUST happen server-side.
        // This is a simplified flow for demo purposes.
        // In production: create a Cloud Function that performs the code exchange securely.
        var formData = new Dictionary<string, string>
        {
            { "code", code },
            { "client_id", AppConfig.GoogleWebClientId },
            { "redirect_uri", redirectUri },
            { "grant_type", "authorization_code" }
        };

        var response = await _http.PostAsync("https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(formData));
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json).RootElement;

        var idToken = doc.TryGetProperty("id_token", out var it) ? it.GetString() ?? "" : "";
        var accessToken = doc.TryGetProperty("access_token", out var at) ? at.GetString() ?? "" : "";
        return (idToken, accessToken);
    }
}

public class GoogleSignInResult
{
    public string IdToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public int ExpiresIn { get; set; } = 3600;
    public bool IsNewUser { get; set; }
}
