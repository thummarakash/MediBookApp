namespace MediBook.Models;

public class Doctor
{
    public int Id { get; set; }
    public string FirestoreId { get; set; } = string.Empty;
    public string ClinicFirestoreId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#155EEF";

    // standard consultation fee; FeePerMinute is used for extended sessions
    public double FeePerAppointment { get; set; } = 80.00;
    public double FeePerMinute { get; set; } = 3.00;
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public string? ScheduleJson { get; set; }

    private WeeklySchedule? _weeklySchedule;
    public WeeklySchedule GetWeeklySchedule()
    {
        if (_weeklySchedule == null)
        {
            _weeklySchedule = WeeklySchedule.FromJson(ScheduleJson);
        }
        return _weeklySchedule;
    }

    public void UpdateSchedule(WeeklySchedule schedule)
    {
        _weeklySchedule = schedule;
        ScheduleJson = schedule.ToJson();
    }

    public string ClinicName { get; set; } = string.Empty;
    public string DistanceText { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "System";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
