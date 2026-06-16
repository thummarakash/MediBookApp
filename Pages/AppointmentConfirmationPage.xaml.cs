using MediBook.Services;

namespace MediBook.Pages;

[QueryProperty(nameof(AppointmentId), "appointmentId")]
public partial class AppointmentConfirmationPage : ContentPage
{
    public string AppointmentId { get; set; } = string.Empty;

    public AppointmentConfirmationPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (int.TryParse(AppointmentId, out var id))
        {
            var appointment = await DatabaseService.Instance.GetAppointmentAsync(id);
            if (appointment != null)
            {
                DoctorLabel.Text = appointment.DoctorName;
                DepartmentLabel.Text = appointment.Department;
                DateTimeLabel.Text = appointment.DisplayDateTime;
                EmailLabel.Text = appointment.EmailReminderQueued ? "Confirmation email opened and day reminder queued" : "Email reminder disabled";
            }
        }
    }

    private async void OnAppointmentsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//appointments");
    private async void OnHomeClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//home");
}
