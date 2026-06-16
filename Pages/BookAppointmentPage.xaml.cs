using MediBook.Models;
using MediBook.Services;

namespace MediBook.Pages;

[QueryProperty(nameof(DoctorId), "doctorId")]
public partial class BookAppointmentPage : ContentPage
{
    private Doctor? _doctor;

    public string DoctorId { get; set; } = string.Empty;

    public BookAppointmentPage()
    {
        InitializeComponent();
        AppointmentDatePicker.MinimumDate = DateTime.Today;
        AppointmentDatePicker.Date = DateTime.Today.AddDays(1);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (int.TryParse(DoctorId, out var doctorId))
        {
            _doctor = await DatabaseService.Instance.GetDoctorAsync(doctorId);
        }

        if (_doctor != null)
        {
            DoctorNameLabel.Text = _doctor.Name;
            DoctorSpecialtyLabel.Text = $"{_doctor.Specialty} • {_doctor.Department}";
            DoctorAvailabilityLabel.Text = _doctor.Availability;
        }
    }

    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        try
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user == null)
            {
                await DisplayAlert("Login required", "Please login before booking an appointment.", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            if (_doctor == null)
            {
                await DisplayAlert("Doctor", "Please select a doctor again.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(ReasonEditor.Text))
            {
                await DisplayAlert("Reason", "Please enter a short reason for your visit.", "OK");
                return;
            }

            var appointment = new Appointment
            {
                UserId = user.Id,
                DoctorId = _doctor.Id,
                DoctorName = _doctor.Name,
                Department = _doctor.Department,
                DateText = $"{AppointmentDatePicker.Date:yyyy-MM-dd}",
                TimeText = $"{AppointmentTimePicker.Time:hh\\:mm}",
                Reason = ReasonEditor.Text.Trim(),
                Status = "Upcoming",
                ReminderEnabled = EmailReminderCheck.IsChecked,
                EmailReminderQueued = EmailReminderCheck.IsChecked,
                CreatedAt = DateTime.Now
            };

            await DatabaseService.Instance.SaveAppointmentAsync(appointment);
            if (EmailReminderCheck.IsChecked)
            {
                await EmailNotificationService.Instance.QueueAndSendAppointmentEmailAsync(user, appointment, _doctor);
            }

            await Shell.Current.GoToAsync($"{nameof(AppointmentConfirmationPage)}?appointmentId={appointment.Id}");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Booking error", ex.Message, "OK");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//doctors");
    }
}
