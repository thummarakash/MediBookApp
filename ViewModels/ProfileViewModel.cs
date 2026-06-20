using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;
using MediBook.Repositories;
using System.Globalization;

namespace MediBook.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty] string userName = "User";
    [ObservableProperty] string userInitials = "US";
    [ObservableProperty] string email = "user@medibook.com";
    [ObservableProperty] string phone = "+61 400 000 000";
    [ObservableProperty] string dateOfBirth = "15/08/1995";
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
                DateOfBirth = string.IsNullOrWhiteSpace(user.DateOfBirth) ? "15/08/1995" : user.DateOfBirth;
                DateOfBirthDate = ParseDate(DateOfBirth);
                AvatarUrl = user.AvatarUrl;
                HasAvatarUrl = !string.IsNullOrEmpty(AvatarUrl);
            }
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
        {
            return;
        }

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

                // Save to Firestore
                await UserRepository.Instance.UpdateAsync(user);
                
                // Refresh initials
                UserInitials = user.Initials;
            }
            IsEditing = false;
        }
        catch (Exception ex)
        {
            // Handle error or set loading false
        }
        finally
        {
            IsLoading = false;
        }
    }

    private DateTime ParseDate(string dateStr)
    {
        if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", null, DateTimeStyles.None, out var dt))
            return dt;
        if (DateTime.TryParse(dateStr, out dt))
            return dt;
        return DateTime.Now;
    }
}
