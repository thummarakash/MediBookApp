using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class AdminDashboardViewModel : ObservableObject
{
    [ObservableProperty] int patientCount;
    [ObservableProperty] int doctorCount;
    [ObservableProperty] int bookingCount;
    [ObservableProperty] bool isLoading;

    [RelayCommand]
    async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var doctorsTask = DatabaseService.Instance.GetDoctorsAsync();
            var appointmentsTask = DatabaseService.Instance.GetAllAppointmentsAsync();
            await Task.WhenAll(doctorsTask, appointmentsTask);

            DoctorCount = doctorsTask.Result.Count;
            BookingCount = appointmentsTask.Result.Count;

            // derive patient count from unique user IDs across all bookings
            // (we don't have a separate users collection to query at the moment)
            int uniqueUsers = appointmentsTask.Result
                .Where(a => !string.IsNullOrEmpty(a.UserFirestoreId))
                .Select(a => a.UserFirestoreId)
                .Distinct()
                .Count();
            PatientCount = Math.Max(uniqueUsers, BookingCount > 0 ? 1 : 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdminDashboard] LoadAsync threw: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
