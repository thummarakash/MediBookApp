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
            var all = await DatabaseService.Instance.GetAllAppointmentsAsync();
            // most recent at top — admins typically need to action the latest bookings first
            Bookings = new ObservableCollection<Appointment>(
                all.OrderByDescending(a => a.CreatedAt));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdminBookings] fetch error: {ex.Message}");
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
