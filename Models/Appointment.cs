using SQLite;

namespace MediBook.Models;

public class Appointment
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    [Indexed]
    public int DoctorId { get; set; }

    public string DoctorName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Upcoming";
    public bool ReminderEnabled { get; set; } = true;
    public bool EmailReminderQueued { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Ignore]
    public string DisplayDateTime => $"{DateText} at {TimeText}";
}
