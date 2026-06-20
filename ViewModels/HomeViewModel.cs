using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    [ObservableProperty] string greeting = "Welcome";
    [ObservableProperty] string dateText = DateTime.Now.ToString("dddd, d MMMM yyyy");
    [ObservableProperty] string userInitials = "US";

    [ObservableProperty] string nextDoctorName = "No appointment booked";
    [ObservableProperty] string nextDoctorDetail = "Book your first appointment today.";
    [ObservableProperty] string nextStatus = "—";
    [ObservableProperty] bool hasNextAppointment;

    [ObservableProperty] ObservableCollection<Appointment> upcomingAppointments = new();

    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isEmpty;

    [RelayCommand]
    async Task SyncDashboardData()
    {
        IsLoading = true;
        
        try
        {
            var u = await DatabaseService.Instance.GetCurrentUserAsync();
            if (u != null)
            {
                var h = DateTime.Now.Hour;
                string greet = h < 12 ? "Good Morning" : (h < 17 ? "Good Afternoon" : "Good Evening");
                
                Greeting = $"{greet}, {u.FullName.Split(' ')[0]}";
                UserInitials = u.Initials;
            }

            var next = await DatabaseService.Instance.GetNextAppointmentAsync();
            
            if (next != null)
            {
                HasNextAppointment = true;
                NextDoctorName = next.DoctorName;
                NextDoctorDetail = $"{next.Department} • {next.DisplayDateTime}";
                NextStatus = $"• {next.Status}";
            }
            else
            {
                HasNextAppointment = false;
                NextDoctorName = "No appointment booked";
                NextDoctorDetail = "Book your first appointment today.";
                NextStatus = "—";
            }

            var list = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            var filtered = new List<Appointment>();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Status == "Upcoming")
                {
                    filtered.Add(list[i]);
                }
            }
            
            UpcomingAppointments = new ObservableCollection<Appointment>(filtered);
            IsEmpty = filtered.Count == 0;

            await EmailNotificationService.Instance.ProcessDueReminderEmailsAsync();
        }
        catch (Exception load_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HOME_LOAD_ERR] Failed: {load_ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
