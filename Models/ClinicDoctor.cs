namespace MediBook.Models;

public class ClinicDoctor
{
    public int Id { get; set; }
    public int ClinicId { get; set; }
    public int DoctorId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "System";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}
