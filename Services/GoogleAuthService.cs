using MediBook.Models;

namespace MediBook.Services;

public class GoogleAuthService
{
    public static GoogleAuthService Instance { get; } = new();

    private GoogleAuthService() { }

    // Pure mock — no real auth
    public async Task<UserAccount> SignInAsync()
    {
        await Task.Delay(300); // Simulate brief delay
        return await DatabaseService.Instance.SaveGoogleUserAsync(
            "Google Patient",
            "google.patient@medibook.app",
            "demo-google-user");
    }
}
