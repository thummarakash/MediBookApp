namespace MediBook.Models;

public class Doctor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#155EEF";
    public double FeePerAppointment { get; set; } = 80.00;
    public double FeePerMinute { get; set; } = 3.00;
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;

    public string ClinicName { get; set; } = string.Empty;
    public string DistanceText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "System";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
