namespace MediBook.Models;

public class MedicalDocument
{
    public int Id { get; set; }
    public string FirestoreId { get; set; } = string.Empty;

    public int UserId { get; set; }
    public string UserFirestoreId { get; set; } = string.Empty;

    public string DocumentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;

    // populated after a successful upload to Firebase Storage
    public string? StorageUrl { get; set; }
    public string? StoragePath { get; set; }

    public string Notes { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.Now;
    public int FileSizeBytes { get; set; }
    public string? MimeType { get; set; }

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

    public bool IsUploaded => !string.IsNullOrEmpty(StorageUrl);
}
