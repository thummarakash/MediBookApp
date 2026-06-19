namespace MediBook.Models;

public class Appointment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int ClinicId { get; set; }
    public double TotalFee { get; set; }
    public string Status { get; set; } = "Upcoming";
    public bool ReminderEnabled { get; set; } = true;
    public bool EmailReminderQueued { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "System";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public DateTime FullDateTime
    {
        get
        {
            if (DateTime.TryParseExact($"{DateText} {TimeText}", "yyyy-MM-dd hh:mm tt", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
            {
                return dt;
            }
            if (DateTime.TryParse(DateText, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
            {
                return d;
            }
            return DateTime.MaxValue;
        }
    }

    public string DisplayDateTime => $"{DateText} at {TimeText}";

    public Color StatusColor => Status switch
    {
        "Completed" => Color.FromArgb("#15803D"),
        "Cancelled" => Color.FromArgb("#DC2626"),
        _ => Color.FromArgb("#C2740C")
    };

    public Color StatusBgColor => Status switch
    {
        "Completed" => Color.FromArgb("#DCFCE7"),
        "Cancelled" => Color.FromArgb("#FEE2E2"),
        _ => Color.FromArgb("#FEF3C7")
    };
}
