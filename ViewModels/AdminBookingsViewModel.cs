using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class AdminBookingsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Appointment> _bookings = new();

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand]
    private async Task LoadBookingsAsync()
    {
        IsLoading = true;
        try
        {
            var appointments = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            Bookings = new ObservableCollection<Appointment>(appointments);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("//admindashboard");
    }
}
