using MediBook.Models;
using MediBook.Services.Email;

namespace MediBook.Services;

public class EmailNotificationService
{
    public static EmailNotificationService Instance { get; } = new();
    private EmailNotificationService() { }

    public async Task QueueAndSendAppointmentEmailAsync(UserAccount user, Appointment appointment, Doctor doctor)
    {
        if (!user.NotificationsEnabled) return;
        if (string.IsNullOrWhiteSpace(user.Email)) return;

        // Fire and forget — email failure should never block appointment booking
        _ = Task.Run(async () =>
        {
            try
            {
                await SmtpEmailService.Instance.SendAppointmentConfirmationAsync(
                    toEmail: user.Email,
                    patientName: user.FullName,
                    doctorName: doctor.Name,
                    clinicName: appointment.ClinicName,
                    dateText: appointment.DateText,
                    timeText: appointment.TimeText,
                    reason: appointment.Reason,
                    fee: appointment.TotalFee);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Email] Confirmation send failed: {ex.Message}");
            }
        });
    }

    public async Task ProcessDueReminderEmailsAsync()
    {
        try
        {
            var user = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user == null || !user.NotificationsEnabled) return;

            var today = DateTime.Today.ToString("yyyy-MM-dd");
            var appointments = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            var todayUpcoming = appointments
                .Where(a => a.Status == "Upcoming" && a.DateText == today && a.ReminderEnabled)
                .ToList();

            foreach (var appt in todayUpcoming)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SmtpEmailService.Instance.SendAppointmentReminderAsync(
                            toEmail: user.Email,
                            patientName: user.FullName,
                            doctorName: appt.DoctorName,
                            timeText: appt.TimeText,
                            clinicName: appt.ClinicName);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Email] Reminder send failed: {ex.Message}");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Email] ProcessDueReminders error: {ex.Message}");
        }
    }
}
