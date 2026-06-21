using MailKit.Net.Smtp;
using MailKit.Security;
using MediBook.Configuration;
using MimeKit;

namespace MediBook.Services.Email;

public class SmtpEmailService
{
    public static SmtpEmailService Instance { get; } = new();
    private SmtpEmailService() { }

    public async Task<bool> SendEmailAsync(string toAddress, string toName, string subject, string htmlBody)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(AppConfig.Smtp.SenderName, AppConfig.Smtp.SenderAddress));
            message.To.Add(new MailboxAddress(toName, toAddress));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(AppConfig.Smtp.Host, AppConfig.Smtp.Port, SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(AppConfig.Smtp.SenderAddress, AppConfig.Smtp.AppPassword);
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SmtpEmailService] Send failed: {ex.Message}");
            return false;
        }
    }

    public async Task SendAppointmentConfirmationAsync(
        string toEmail,
        string patientName,
        string doctorName,
        string clinicName,
        string dateText,
        string timeText,
        string reason,
        double fee)
    {
        var subject = $"MediBook — Appointment Confirmed with {doctorName}";
        var body = EmailTemplateService.AppointmentConfirmation(
            patientName, doctorName, clinicName, dateText, timeText, reason, fee);
        await SendEmailAsync(toEmail, patientName, subject, body);
    }

    public async Task SendAppointmentReminderAsync(
        string toEmail,
        string patientName,
        string doctorName,
        string timeText,
        string clinicName)
    {
        var subject = $"MediBook Reminder — Appointment Today at {timeText}";
        var body = EmailTemplateService.AppointmentReminder(patientName, doctorName, timeText, clinicName);
        await SendEmailAsync(toEmail, patientName, subject, body);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string fullName)
    {
        var subject = "Welcome to MediBook!";
        var body = EmailTemplateService.WelcomeEmail(fullName);
        await SendEmailAsync(toEmail, fullName, subject, body);
    }

    public async Task SendPasswordResetNotificationAsync(string toEmail, string displayName)
    {
        var subject = "MediBook — Password Reset Requested";
        var body = EmailTemplateService.PasswordResetConfirmation(displayName);
        await SendEmailAsync(toEmail, displayName, subject, body);
    }
}
