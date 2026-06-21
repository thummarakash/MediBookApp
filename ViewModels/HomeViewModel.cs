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
    async Task LoadDashboardData()
    {
        IsLoading = true;
        try
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user != null)
            {
                int hr = DateTime.Now.Hour;
                // afternoon ends at 5pm — "Good Evening" after that
                string timeGreeting = hr < 12 ? "Good Morning" : hr < 17 ? "Good Afternoon" : "Good Evening";
                Greeting = $"{timeGreeting}, {user.FullName.Split(' ')[0]}";
                UserInitials = user.Initials;
            }

            var nextAppointment = await DatabaseService.Instance.GetNextAppointmentAsync();
            if (nextAppointment != null)
            {
                HasNextAppointment = true;
                NextDoctorName = nextAppointment.DoctorName;
                NextDoctorDetail = $"{nextAppointment.Department} • {nextAppointment.DisplayDateTime}";
                NextStatus = $"• {nextAppointment.Status}";
            }
            else
            {
                HasNextAppointment = false;
                NextDoctorName = "No appointment booked";
                NextDoctorDetail = "Book your first appointment today.";
                NextStatus = "—";
            }

            var appointments = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            var upcomingList = new List<Appointment>();
            for (int i = 0; i < appointments.Count; i++)
            {
                if (appointments[i].Status == "Upcoming")
                    upcomingList.Add(appointments[i]);
            }

            UpcomingAppointments = new ObservableCollection<Appointment>(upcomingList);
            IsEmpty = upcomingList.Count == 0;

            // home page loads on every login so this is the natural place to fire reminder emails
            await EmailNotificationService.Instance.ProcessDueReminderEmailsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[HomeVM] dashboard load error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
