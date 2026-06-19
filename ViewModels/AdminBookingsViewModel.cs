using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class AdminBookingsViewModel : ObservableObject
{
    [ObservableProperty] ObservableCollection<Appointment> bookings = new();
    [ObservableProperty] bool isLoading;

    [RelayCommand]
    async Task LoadBookingsAsync()
    {
        IsLoading = true;
        try
        {
            // Admin sees ALL appointments across all users
            var all = await DatabaseService.Instance.GetAllAppointmentsAsync();
            Bookings = new ObservableCollection<Appointment>(
                all.OrderByDescending(a => a.CreatedAt));
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task BackAsync()
        => await Shell.Current.GoToAsync("//admindashboard");
}
