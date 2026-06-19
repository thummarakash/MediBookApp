using MediBook.Models;

namespace MediBook.Services;

public class EmailNotificationService
{
    public static EmailNotificationService Instance { get; } = new();

    private EmailNotificationService() { }

    // Mock — just saves the reminder, no real email
    public async Task QueueAndSendAppointmentEmailAsync(UserAccount user, Appointment appointment, Doctor doctor)
    {
        var reminder = new EmailReminder
        {
            UserId = user.Id,
            AppointmentId = appointment.Id,
            EmailAddress = user.Email,
            Subject = $"Reminder: MediBook appointment today at {appointment.TimeText}",
            Body = $"Hello {user.FullName}, your appointment with {doctor.Name} is confirmed.",
            DueDateText = appointment.DateText,
            Status = "Pending",
            CreatedAt = DateTime.Now
        };

        await DatabaseService.Instance.SaveEmailReminderAsync(reminder);
    }

    // Mock — processes due reminders (marks as sent)
    public async Task ProcessDueReminderEmailsAsync()
    {
        var dueEmails = await DatabaseService.Instance.GetDueEmailRemindersAsync();
        foreach (var email in dueEmails)
        {
            await DatabaseService.Instance.MarkEmailReminderSentAsync(email);
        }
    }
}
