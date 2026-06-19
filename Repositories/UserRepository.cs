using MediBook.Configuration;
using MediBook.Extensions;
using MediBook.Models;
using MediBook.Services.Firebase;

namespace MediBook.Repositories;

public class UserRepository
{
    public static UserRepository Instance { get; } = new();
    private UserRepository() { }

    public async Task<UserAccount?> GetByIdAsync(string uid)
    {
        var doc = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Users, uid);
        if (doc == null) return null;

        var fields = doc.Value.TryGetProperty("fields", out var f) ? f : default;
        return MapFromFirestore(uid, fields);
    }

    public async Task CreateAsync(UserAccount user)
    {
        var fields = MapToFirestore(user);
        await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Users, user.FirestoreId, fields);
    }

    public async Task UpdateAsync(UserAccount user)
    {
        var fields = MapToFirestore(user);
        await FirestoreService.Instance.UpdateDocumentAsync(AppConfig.Collections.Users, user.FirestoreId, fields);
    }

    public async Task UpdateFcmTokenAsync(string uid, string token)
    {
        await FirestoreService.Instance.UpdateDocumentAsync(
            AppConfig.Collections.Users, uid,
            new Dictionary<string, object> { { "fcmToken", token } });
    }

    public async Task UpdateBiometricSettingAsync(string uid, bool enabled)
    {
        await FirestoreService.Instance.UpdateDocumentAsync(
            AppConfig.Collections.Users, uid,
            new Dictionary<string, object> { { "biometricEnabled", enabled } });
    }

    private static UserAccount MapFromFirestore(string uid, System.Text.Json.JsonElement fields)
    {
        return new UserAccount
        {
            FirestoreId = uid,
            Email = FirestoreService.GetString(fields, "email"),
            FullName = FirestoreService.GetString(fields, "fullName"),
            Phone = FirestoreService.GetString(fields, "phone"),
            DateOfBirth = FirestoreService.GetString(fields, "dateOfBirth"),
            Role = FirestoreService.GetString(fields, "role").IfEmpty("Patient"),
            AuthProvider = FirestoreService.GetString(fields, "authProvider").IfEmpty("Local"),
            AvatarColor = FirestoreService.GetString(fields, "avatarColor").IfEmpty("#155EEF"),
            FCMToken = FirestoreService.GetString(fields, "fcmToken"),
            NotificationsEnabled = FirestoreService.GetBool(fields, "notificationsEnabled", true),
            BiometricEnabled = FirestoreService.GetBool(fields, "biometricEnabled"),
            CreatedAt = FirestoreService.GetDateTime(fields, "createdAt"),
            UpdatedAt = FirestoreService.GetDateTime(fields, "updatedAt")
        };
    }

    private static Dictionary<string, object> MapToFirestore(UserAccount user) => new()
    {
        { "email", user.Email },
        { "fullName", user.FullName },
        { "phone", user.Phone },
        { "dateOfBirth", user.DateOfBirth },
        { "role", user.Role },
        { "authProvider", user.AuthProvider },
        { "avatarColor", user.AvatarColor },
        { "fcmToken", user.FCMToken },
        { "notificationsEnabled", user.NotificationsEnabled },
        { "biometricEnabled", user.BiometricEnabled },
        { "createdAt", user.CreatedAt },
        { "updatedAt", DateTime.UtcNow }
    };
}

