using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;
using MediBook.Repositories;
using System.Globalization;

namespace MediBook.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty] string userName = "";
    [ObservableProperty] string userInitials = "";
    [ObservableProperty] string email = "";
    [ObservableProperty] string phone = "";
    [ObservableProperty] string dateOfBirth = "";
    [ObservableProperty] DateTime dateOfBirthDate = DateTime.Now;
    [ObservableProperty] string avatarUrl = "";
    [ObservableProperty] double avatarScale = 1.0;
    [ObservableProperty] double avatarX = 0.0;
    [ObservableProperty] double avatarY = 0.0;
    [ObservableProperty] double avatarRotation = 0.0;
    [ObservableProperty] bool isAvatarPreviewVisible;
    [ObservableProperty] string tempAvatarUrl = "";
    [ObservableProperty] double tempScale = 1.0;
    [ObservableProperty] double tempX = 0.0;
    [ObservableProperty] double tempY = 0.0;
    [ObservableProperty] double tempRotation = 0.0;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInitials))]
    bool hasAvatarUrl;

    public bool ShowInitials => !HasAvatarUrl;

    [ObservableProperty] bool isLoading;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotEditing))]
    bool isEditing;

    public bool IsNotEditing => !IsEditing;

    [ObservableProperty] bool showProfileSetupAlert;
    [ObservableProperty] bool notificationsEnabled;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAdmin))]
    bool isAdmin;

    public bool IsNotAdmin => !IsAdmin;

    partial void OnNotificationsEnabledChanged(bool value)
    {
        Microsoft.Maui.Storage.Preferences.Default.Set("notifications_enabled", value);
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user != null)
            {
                UserName = user.FullName;
                UserInitials = user.Initials;
                Email = user.Email;
                Phone = string.IsNullOrWhiteSpace(user.Phone) ? "" : user.Phone;
                DateOfBirth = string.IsNullOrWhiteSpace(user.DateOfBirth) ? "" : user.DateOfBirth;
                DateOfBirthDate = ParseDate(DateOfBirth);
                AvatarUrl = user.AvatarUrl;
                AvatarScale = user.AvatarScale == 0 ? 1.0 : user.AvatarScale;
                AvatarX = user.AvatarX;
                AvatarY = user.AvatarY;
                AvatarRotation = user.AvatarRotation;
                HasAvatarUrl = !string.IsNullOrEmpty(AvatarUrl) && (AvatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || AvatarUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase));

                IsAdmin = user.Role == "Admin";
                NotificationsEnabled = Microsoft.Maui.Storage.Preferences.Default.Get("notifications_enabled", true);

                // Show setup alert if phone or date of birth is missing
                ShowProfileSetupAlert = (string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(user.DateOfBirth)) && !IsAdmin;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileVM] LoadAsync failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public void StartEdit()
    {
        DateOfBirthDate = ParseDate(DateOfBirth);
        IsEditing = true;
    }

    [RelayCommand]
    public async Task CancelEditAsync()
    {
        IsEditing = false;
        await LoadAsync();
    }

    [RelayCommand]
    public async Task SaveChangesAsync()
    {
        if (string.IsNullOrWhiteSpace(UserName))
            return;

        IsLoading = true;
        try
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user != null)
            {
                user.FullName = UserName;
                user.Phone = Phone;
                DateOfBirth = DateOfBirthDate.ToString("dd/MM/yyyy");
                user.DateOfBirth = DateOfBirth;
                user.AvatarScale = AvatarScale;
                user.AvatarX = AvatarX;
                user.AvatarY = AvatarY;
                user.AvatarRotation = AvatarRotation;

                await UserRepository.Instance.UpdateAsync(user);
                UserInitials = user.Initials;

                ShowProfileSetupAlert = string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(DateOfBirth);
            }
            IsEditing = false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ProfileVM] SaveChangesAsync failed: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // tries the app's expected format first, falls back to TryParse for any ISO or locale variant
    private DateTime ParseDate(string dateStr)
    {
        if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParse(dateStr, out dt))
            return dt;
        return DateTime.Now;
    }
}
