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
                HasAvatarUrl = !string.IsNullOrEmpty(AvatarUrl) && (AvatarUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase) || AvatarUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase));

                // Show setup alert if phone or date of birth is missing
                ShowProfileSetupAlert = string.IsNullOrWhiteSpace(Phone) || string.IsNullOrWhiteSpace(user.DateOfBirth);
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
