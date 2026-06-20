using MediBook.Models;
using MediBook.Services.Email;

namespace MediBook.Services;

public class EmailNotificationService
{
    public static EmailNotificationService Instance { get; } = new();
    private EmailNotificationService() { }

    public async Task QueueAndSendAppointmentEmailAsync(UserAccount user_acc, Appointment appt_info, Doctor doc_info)
    {
        if (!user_acc.NotificationsEnabled) return;
        if (string.IsNullOrWhiteSpace(user_acc.Email)) return;

        // Fire and forget — email failure should never block appointment booking
        _ = Task.Run(async () =>
        {
            try
            {
                await SmtpEmailService.Instance.SendAppointmentConfirmationAsync(
                    toEmail: user_acc.Email,
                    patientName: user_acc.FullName,
                    doctorName: doc_info.Name,
                    clinicName: appt_info.ClinicName,
                    dateText: appt_info.DateText,
                    timeText: appt_info.TimeText,
                    reason: appt_info.Reason,
                    fee: appt_info.TotalFee);
            }
            catch (Exception email_ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Email] Confirmation send failed: {email_ex.Message}");
            }
        });
    }

    public async Task ProcessDueReminderEmailsAsync()
    {
        try
        {
            var user_acc = await DatabaseService.Instance.GetCurrentUserAsync();
            if (user_acc == null || !user_acc.NotificationsEnabled) return;

            var today_date_str = DateTime.Today.ToString("yyyy-MM-dd");
            var appointment_list = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            var today_upcoming_list = appointment_list
                .Where(a => a.Status == "Upcoming" && a.DateText == today_date_str && a.ReminderEnabled)
                .ToList();

            foreach (var booking in today_upcoming_list)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SmtpEmailService.Instance.SendAppointmentReminderAsync(
                            toEmail: user_acc.Email,
                            patientName: user_acc.FullName,
                            doctorName: booking.DoctorName,
                            timeText: booking.TimeText,
                            clinicName: booking.ClinicName);
                    }
                    catch (Exception reminder_ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Email] Reminder send failed: {reminder_ex.Message}");
                    }
                });
            }
        }
        catch (Exception process_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Email] ProcessDueReminders error: {process_ex.Message}");
        }
    }
}
