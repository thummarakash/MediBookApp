using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class AdminDashboardViewModel : ObservableObject
{
    [ObservableProperty] int patientCount = 142;
    [ObservableProperty] int doctorCount;
    [ObservableProperty] int bookingCount;
    [ObservableProperty] bool isLoading;

    [RelayCommand]
    async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var doctors = await DatabaseService.Instance.GetDoctorsAsync();
            DoctorCount = doctors.Count;

            var bookings = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            BookingCount = bookings.Count;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
