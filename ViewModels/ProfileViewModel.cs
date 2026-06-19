using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty] string userName = "User";
    [ObservableProperty] string userInitials = "US";
    [ObservableProperty] string email = "user@medibook.com";
    [ObservableProperty] string phone = "+61 400 000 000";
    [ObservableProperty] string dateOfBirth = "15/08/1995";
    [ObservableProperty] bool isLoading;

    [RelayCommand]
    async Task LoadAsync()
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
                Phone = string.IsNullOrWhiteSpace(user.Phone) ? "+61 400 000 000" : user.Phone;
                DateOfBirth = string.IsNullOrWhiteSpace(user.DateOfBirth) ? "15/08/1995" : user.DateOfBirth;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}
