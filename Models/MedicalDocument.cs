using SQLite;

namespace MediBook.Models;

public class MedicalDocument
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int UserId { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.Now;

    [Ignore]
    public string UploadedDateText => UploadedAt.ToString("dd MMM yyyy");
}
