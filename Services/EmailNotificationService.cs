using MediBook.Models;

namespace MediBook.Services;

public class EmailNotificationService
{
    public static EmailNotificationService Instance { get; } = new();

    private EmailNotificationService()
    {
    }

    public async Task QueueAndSendAppointmentEmailAsync(UserAccount user, Appointment appointment, Doctor doctor)
    {
        var subject = $"MediBook appointment confirmed - {appointment.DateText}";
        var body = $@"Hello {user.FullName},

Your MediBook appointment has been confirmed.

Doctor: {doctor.Name}
Department: {doctor.Department}
Date: {appointment.DateText}
Time: {appointment.TimeText}
Reason: {appointment.Reason}

You will also receive an appointment-day reminder when you open the app on the appointment date.

Thank you,
MediBook Medical Centre";

        var reminder = new EmailReminder
        {
            UserId = user.Id,
            AppointmentId = appointment.Id,
            EmailAddress = user.Email,
            Subject = $"Reminder: MediBook appointment today at {appointment.TimeText}",
            Body = $@"Hello {user.FullName},

This is your MediBook appointment reminder for today.

Doctor: {doctor.Name}
Department: {doctor.Department}
Time: {appointment.TimeText}

Please arrive 10 minutes early.

MediBook Medical Centre",
            DueDateText = appointment.DateText,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        await DatabaseService.Instance.SaveEmailReminderAsync(reminder);
        await NativeActionService.Instance.ComposeEmailAsync(user.Email, subject, body);
    }

    public async Task ProcessDueReminderEmailsAsync()
    {
        var dueEmails = await DatabaseService.Instance.GetDueEmailRemindersAsync();
        foreach (var email in dueEmails)
        {
            await NativeActionService.Instance.ComposeEmailAsync(email.EmailAddress, email.Subject, email.Body);
            await DatabaseService.Instance.MarkEmailReminderSentAsync(email);
        }
    }
}
