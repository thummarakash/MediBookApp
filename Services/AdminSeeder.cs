using System.Diagnostics;
using MediBook.Models;
using MediBook.Repositories;
using MediBook.Services.Auth;
using MediBook.Services.Firebase;

namespace MediBook.Services;

public static class AdminSeeder
{
    private const string AdminEmail = "akashthummarau@gmail.com";
    private const string AdminPassword = "Admin@!23";

    public static async Task SeedAdminAsync()
    {
        try
        {
            Debug.WriteLine("[AdminSeeder] Starting admin check/seed...");

            string userId = null;

            // 1. Try to sign in first to see if the user is already in Firebase Authentication
            try
            {
                var authResult = await FirebaseAuthService.Instance.SignInWithEmailPasswordAsync(AdminEmail, AdminPassword);
                userId = authResult.UserId;
                Debug.WriteLine($"[AdminSeeder] Signed in successfully. Auth UserId: {userId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AdminSeeder] SignIn failed (User may not exist in Auth yet or password mismatch): {ex.Message}");
            }

            // 2. If sign in failed (user doesn't exist in Auth), attempt to sign up/register
            if (string.IsNullOrEmpty(userId))
            {
                try
                {
                    var authResult = await FirebaseAuthService.Instance.SignUpWithEmailPasswordAsync(AdminEmail, AdminPassword, "System Admin");
                    userId = authResult.UserId;
                    Debug.WriteLine($"[AdminSeeder] Registered in Auth successfully. Auth UserId: {userId}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AdminSeeder] SignUp failed: {ex.Message}");
                }
            }

            // 3. If we successfully have a userId (either from SignIn or SignUp)
            if (!string.IsNullOrEmpty(userId))
            {
                // Check if user exists in Firestore
                var existingUser = await UserRepository.Instance.GetByIdAsync(userId);
                if (existingUser == null)
                {
                    Debug.WriteLine("[AdminSeeder] Admin user profile not found in Firestore. Seeding now...");
                    var adminUser = new UserAccount
                    {
                        FirestoreId = userId,
                        Id = 1,
                        FullName = "System Admin",
                        Email = AdminEmail,
                        Phone = "+91 9999999999",
                        DateOfBirth = "01/01/1990",
                        Role = "Admin",
                        AuthProvider = "Local",
                        AvatarColor = "#155EEF",
                        CreatedAt = DateTime.UtcNow
                    };
                    await UserRepository.Instance.CreateAsync(adminUser);
                    Debug.WriteLine("[AdminSeeder] Admin user profile seeded successfully in Firestore.");
                }
                else
                {
                    // Ensure the role is Admin if it was somehow changed or already exists
                    if (existingUser.Role != "Admin")
                    {
                        Debug.WriteLine("[AdminSeeder] Admin user profile exists but role is not Admin. Updating to Admin...");
                        existingUser.Role = "Admin";
                        await UserRepository.Instance.UpdateAsync(existingUser);
                        Debug.WriteLine("[AdminSeeder] Admin role updated successfully.");
                    }
                    else
                    {
                        Debug.WriteLine("[AdminSeeder] Admin user profile already exists in Firestore with correct role.");
                    }
                }
            }
            else
            {
                Debug.WriteLine("[AdminSeeder] Failed to obtain Auth UID for admin user.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AdminSeeder] Error during seeding: {ex.Message}");
        }
    }
}
