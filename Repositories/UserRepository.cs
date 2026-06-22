using MediBook.Configuration;
using MediBook.Extensions;
using MediBook.Models;
using MediBook.Services.Firebase;

namespace MediBook.Repositories;

public class UserRepository
{
    public static UserRepository Instance { get; } = new();
    private UserRepository() { }

    public async Task<UserAccount?> GetByIdAsync(string userId)
    {
        try
        {
            var snapshot = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Users, userId);
            if (snapshot == null) return null;

            var fields = snapshot.Value.TryGetProperty("fields", out var f) ? f : default;
            return MapFromFirestore(userId, fields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepo] GetByIdAsync ({userId}): {ex.Message}");
            return null;
        }
    }

    public async Task CreateAsync(UserAccount account)
    {
        try
        {
            var firestoreFields = MapToFirestore(account);
            await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Users, account.FirestoreId, firestoreFields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepo] CreateAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(UserAccount account)
    {
        try
        {
            var firestoreFields = MapToFirestore(account);
            await FirestoreService.Instance.UpdateDocumentAsync(AppConfig.Collections.Users, account.FirestoreId, firestoreFields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepo] UpdateAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateFcmTokenAsync(string userId, string fcmToken)
    {
        try
        {
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Users, userId,
                new Dictionary<string, object> { { "fcmToken", fcmToken } });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepo] FCM token update failed for {userId}: {ex.Message}");
        }
    }

    public async Task<System.Collections.Generic.List<UserAccount>> GetAllAsync()
    {
        try
        {
            var docs = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Users);
            var results = new System.Collections.Generic.List<UserAccount>();
            foreach (var doc in docs)
            {
                var fields = doc.Fields;
                results.Add(MapFromFirestore(doc.Id, fields));
            }
            return results;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepo] GetAllAsync failed: {ex.Message}");
            return new System.Collections.Generic.List<UserAccount>();
        }
    }

    public async Task UpdateBiometricSettingAsync(string userId, bool enabled)
    {
        try
        {
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Users, userId,
                new Dictionary<string, object> { { "biometricEnabled", enabled } });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepo] biometric setting update failed ({userId}): {ex.Message}");
        }
    }

    private static UserAccount MapFromFirestore(string userId, System.Text.Json.JsonElement fields)
    {
        return new UserAccount
        {
            FirestoreId = userId,
            Email = FirestoreService.GetString(fields, "email"),
            FullName = FirestoreService.GetString(fields, "fullName"),
            Phone = FirestoreService.GetString(fields, "phone"),
            DateOfBirth = FirestoreService.GetString(fields, "dateOfBirth"),
            Role = FirestoreService.GetString(fields, "role").IfEmpty("Patient"),
            AuthProvider = FirestoreService.GetString(fields, "authProvider").IfEmpty("Local"),
            AvatarColor = FirestoreService.GetString(fields, "avatarColor").IfEmpty("#155EEF"),
            AvatarUrl = FirestoreService.GetString(fields, "avatarUrl"),
            AvatarScale = FirestoreService.GetDouble(fields, "avatarScale", 1.0),
            AvatarX = FirestoreService.GetDouble(fields, "avatarX", 0.0),
            AvatarY = FirestoreService.GetDouble(fields, "avatarY", 0.0),
            AvatarRotation = FirestoreService.GetDouble(fields, "avatarRotation", 0.0),
            FCMToken = FirestoreService.GetString(fields, "fcmToken"),
            NotificationsEnabled = FirestoreService.GetBool(fields, "notificationsEnabled", true),
            BiometricEnabled = FirestoreService.GetBool(fields, "biometricEnabled"),
            CreatedAt = FirestoreService.GetDateTime(fields, "createdAt"),
            UpdatedAt = FirestoreService.GetDateTime(fields, "updatedAt")
        };
    }
 
    private static Dictionary<string, object> MapToFirestore(UserAccount account) => new()
    {
        { "email", account.Email },
        { "fullName", account.FullName },
        { "phone", account.Phone },
        { "dateOfBirth", account.DateOfBirth },
        { "role", account.Role },
        { "authProvider", account.AuthProvider },
        { "avatarColor", account.AvatarColor },
        { "avatarUrl", account.AvatarUrl ?? string.Empty },
        { "avatarScale", account.AvatarScale },
        { "avatarX", account.AvatarX },
        { "avatarY", account.AvatarY },
        { "avatarRotation", account.AvatarRotation },
        { "fcmToken", account.FCMToken },
        { "notificationsEnabled", account.NotificationsEnabled },
        { "biometricEnabled", account.BiometricEnabled },
        { "createdAt", account.CreatedAt },
        { "updatedAt", DateTime.UtcNow }
    };
}
