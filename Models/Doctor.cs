using SQLite;

namespace MediBook.Models;

public class Doctor
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#155EEF";
    public bool IsActive { get; set; } = true;
}
