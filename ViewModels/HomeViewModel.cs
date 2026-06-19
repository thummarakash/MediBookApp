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
    async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user != null)
            {
                var hour = DateTime.Now.Hour;
                var greet = hour < 12 ? "Good Morning" : hour < 17 ? "Good Afternoon" : "Good Evening";
                Greeting = $"{greet}, {user.FullName.Split(' ')[0]}";
                UserInitials = user.Initials;
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

            var appointments = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            var upcoming = appointments.Where(a => a.Status == "Upcoming").ToList();
            UpcomingAppointments = new ObservableCollection<Appointment>(upcoming);
            IsEmpty = !upcoming.Any();

            await EmailNotificationService.Instance.ProcessDueReminderEmailsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
