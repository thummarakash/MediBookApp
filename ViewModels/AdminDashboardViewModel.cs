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
            var usersTask = DatabaseService.Instance.GetAllUsersAsync();
            await Task.WhenAll(doctorsTask, appointmentsTask, usersTask);

            DoctorCount = doctorsTask.Result.Count;
            BookingCount = appointmentsTask.Result.Count;

            var patients = usersTask.Result.Where(u => u.Role == "Patient").ToList();
            PatientCount = patients.Count;
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
