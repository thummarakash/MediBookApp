using MailKit.Net.Smtp;
using MailKit.Security;
using MediBook.Configuration;
using MimeKit;

namespace MediBook.Services.Email;

/// <summary>
/// Sends HTML emails via Gmail SMTP using MailKit.
/// NOTE: For production, move this to a secure backend (Firebase Cloud Function / Azure Function).
///       Storing SMTP credentials in the app exposes them to APK extraction.
/// </summary>
public class SmtpEmailService
{
    public static SmtpEmailService Instance { get; } = new();
    private SmtpEmailService() { }

    public async Task<bool> SendEmailAsync(string toAddress, string toName, string subject, string htmlBody)
    {
        try
        {
            var mime_msg = new MimeMessage();
            mime_msg.From.Add(new MailboxAddress(AppConfig.Smtp.SenderName, AppConfig.Smtp.SenderAddress));
            mime_msg.To.Add(new MailboxAddress(toName, toAddress));
            mime_msg.Subject = subject;

            var body_builder = new BodyBuilder { HtmlBody = htmlBody };
            mime_msg.Body = body_builder.ToMessageBody();

            using var smtp_client = new SmtpClient();
            await smtp_client.ConnectAsync(AppConfig.Smtp.Host, AppConfig.Smtp.Port, SecureSocketOptions.StartTls);
            await smtp_client.AuthenticateAsync(AppConfig.Smtp.SenderAddress, AppConfig.Smtp.AppPassword);
            await smtp_client.SendAsync(mime_msg);
            await smtp_client.DisconnectAsync(true);
            return true;
        }
        catch (Exception smtp_ex)
        {
            // Standard console logging for SMTP delivery errors
            System.Diagnostics.Debug.WriteLine($"[SMTP] Send failed: {smtp_ex.Message}");
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
