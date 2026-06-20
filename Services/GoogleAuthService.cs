using MediBook.Models;
using MediBook.Services.Auth;
using MediBook.Services.Firebase;

namespace MediBook.Services;

public class GoogleAuthService
{
    public static GoogleAuthService Instance { get; } = new();
    private GoogleAuthService() { }

    public async Task<UserAccount> SignInAsync()
    {
        var result = await GoogleSignInService.Instance.SignInAsync();

        var authResult = new AuthResult
        {
            IdToken = result.IdToken,
            RefreshToken = result.RefreshToken,
            UserId = result.UserId,
            Email = result.Email,
            DisplayName = result.DisplayName,
            ExpiresIn = result.ExpiresIn,
            ExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn)
        };

        // Save session first so Firestore operations in SaveGoogleUserAsync are authenticated
        await SessionService.Instance.SaveSessionAsync(authResult, "Patient");

        var user = await DatabaseService.Instance.SaveGoogleUserAsync(
            result.DisplayName, result.Email, result.UserId, result.PhotoUrl);

        return user;
    }
}
