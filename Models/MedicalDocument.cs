namespace MediBook.Models;

public class MedicalDocument
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "System";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public string UploadedDateText => UploadedAt.ToString("dd MMM yyyy");

    public string IconImage => DocumentType switch
    {
        "Prescription" => "icon_prescription.png",
        "Blood Test" => "icon_blood_test.png",
        _ => "icon_document.png"
    };
}
