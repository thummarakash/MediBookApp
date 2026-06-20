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

            // Patient count is estimated from unique user IDs in appointments
            var uniqueUsers = appointmentsTask.Result
                .Where(a => !string.IsNullOrEmpty(a.UserFirestoreId))
                .Select(a => a.UserFirestoreId)
                .Distinct()
                .Count();
            PatientCount = Math.Max(uniqueUsers, BookingCount > 0 ? 1 : 0);
        }
        catch (Exception dash_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AdminDashVM] Failed to compile admin dashboard stats: {dash_ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
