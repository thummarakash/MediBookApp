using MediBook.Configuration;
using MediBook.Extensions;
using MediBook.Models;
using MediBook.Services.Firebase;

namespace MediBook.Repositories;

public class UserRepository
{
    public static UserRepository Instance { get; } = new();
    private UserRepository() { }

    public async Task<UserAccount?> GetByIdAsync(string user_id)
    {
        try
        {
            var doc_snap = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Users, user_id);
            if (doc_snap == null) return null;

            var field_map = doc_snap.Value.TryGetProperty("fields", out var fields) ? fields : default;
            return MapFromFirestore(user_id, field_map);
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepository] GetByIdAsync query failed for {user_id}: {read_ex.Message}");
            return null;
        }
    }

    public async Task CreateAsync(UserAccount account_data)
    {
        try
        {
            var mapped_fields = MapToFirestore(account_data);
            await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Users, account_data.FirestoreId, mapped_fields);
        }
        catch (Exception write_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepository] CreateAsync error: {write_ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(UserAccount account_data)
    {
        try
        {
            var mapped_fields = MapToFirestore(account_data);
            await FirestoreService.Instance.UpdateDocumentAsync(AppConfig.Collections.Users, account_data.FirestoreId, mapped_fields);
        }
        catch (Exception update_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepository] UpdateAsync error: {update_ex.Message}");
            throw;
        }
    }

    public async Task UpdateFcmTokenAsync(string user_id, string fcm_tok)
    {
        try
        {
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Users, user_id,
                new Dictionary<string, object> { { "fcmToken", fcm_tok } });
        }
        catch (Exception update_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepository] UpdateFcmTokenAsync failed: {update_ex.Message}");
        }
    }

    public async Task UpdateBiometricSettingAsync(string user_id, bool is_on)
    {
        try
        {
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Users, user_id,
                new Dictionary<string, object> { { "biometricEnabled", is_on } });
        }
        catch (Exception update_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserRepository] UpdateBiometricSettingAsync failed: {update_ex.Message}");
        }
    }

    private static UserAccount MapFromFirestore(string user_id, System.Text.Json.JsonElement payload_fields)
    {
        return new UserAccount
        {
            FirestoreId = user_id,
            Email = FirestoreService.GetString(payload_fields, "email"),
            FullName = FirestoreService.GetString(payload_fields, "fullName"),
            Phone = FirestoreService.GetString(payload_fields, "phone"),
            DateOfBirth = FirestoreService.GetString(payload_fields, "dateOfBirth"),
            Role = FirestoreService.GetString(payload_fields, "role").IfEmpty("Patient"),
            AuthProvider = FirestoreService.GetString(payload_fields, "authProvider").IfEmpty("Local"),
            AvatarColor = FirestoreService.GetString(payload_fields, "avatarColor").IfEmpty("#155EEF"),
            FCMToken = FirestoreService.GetString(payload_fields, "fcmToken"),
            NotificationsEnabled = FirestoreService.GetBool(payload_fields, "notificationsEnabled", true),
            BiometricEnabled = FirestoreService.GetBool(payload_fields, "biometricEnabled"),
            CreatedAt = FirestoreService.GetDateTime(payload_fields, "createdAt"),
            UpdatedAt = FirestoreService.GetDateTime(payload_fields, "updatedAt")
        };
    }

    private static Dictionary<string, object> MapToFirestore(UserAccount account_data) => new()
    {
        { "email", account_data.Email },
        { "fullName", account_data.FullName },
        { "phone", account_data.Phone },
        { "dateOfBirth", account_data.DateOfBirth },
        { "role", account_data.Role },
        { "authProvider", account_data.AuthProvider },
        { "avatarColor", account_data.AvatarColor },
        { "fcmToken", account_data.FCMToken },
        { "notificationsEnabled", account_data.NotificationsEnabled },
        { "biometricEnabled", account_data.BiometricEnabled },
        { "createdAt", account_data.CreatedAt },
        { "updatedAt", DateTime.UtcNow }
    };
}
