using Microsoft.Maui.Authentication;
using MediBook.Models;

namespace MediBook.Services;

public class GoogleAuthService
{
    public static GoogleAuthService Instance { get; } = new();

    // Replace this with your Google OAuth client ID to enable real Google OAuth.
    private const string GoogleClientId = "PASTE_GOOGLE_OAUTH_CLIENT_ID_HERE";
    private const string RedirectUri = "com.akash.medibook:/oauth2redirect";

    private GoogleAuthService()
    {
    }

    public async Task<UserAccount> SignInAsync()
    {
        if (GoogleClientId.StartsWith("PASTE_", StringComparison.OrdinalIgnoreCase))
        {
            return await DatabaseService.Instance.SaveGoogleUserAsync("Google Patient", "google.patient@medibook.app", "demo-google-user");
        }

        var authUrl = new Uri("https://accounts.google.com/o/oauth2/v2/auth" +
                              $"?client_id={Uri.EscapeDataString(GoogleClientId)}" +
                              $"&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
                              "&response_type=token" +
                              $"&scope={Uri.EscapeDataString("openid email profile")}");
        var callbackUrl = new Uri(RedirectUri);
        var result = await WebAuthenticator.Default.AuthenticateAsync(authUrl, callbackUrl);

        var email = result.Properties.TryGetValue("email", out var returnedEmail)
            ? returnedEmail
            : "google.patient@medibook.app";
        var name = result.Properties.TryGetValue("name", out var returnedName)
            ? returnedName
            : "Google Patient";
        var subject = result.Properties.TryGetValue("sub", out var returnedSubject)
            ? returnedSubject
            : email;

        return await DatabaseService.Instance.SaveGoogleUserAsync(name, email, subject);
    }
}
