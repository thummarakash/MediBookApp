namespace MediBook.Models;

public class EmailReminder
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int AppointmentId { get; set; }
    public string EmailAddress { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string DueDateText { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? SentAt { get; set; }
}
