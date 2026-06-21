using MediBook.Configuration;
using MediBook.Models;
using MediBook.Services.Firebase;

namespace MediBook.Repositories;

public class DocumentRepository
{
    public static DocumentRepository Instance { get; } = new();
    private DocumentRepository() { }

    public async Task<string> CreateAsync(MedicalDocument document)
    {
        try
        {
            var firestoreFields = MapToFirestore(document);
            var newDocId = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.MedicalDocuments, firestoreFields);
            document.FirestoreId = newDocId;
            return newDocId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentRepository] CreateAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(MedicalDocument document)
    {
        try
        {
            if (string.IsNullOrEmpty(document.FirestoreId)) return;
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.MedicalDocuments,
                document.FirestoreId,
                MapToFirestore(document));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentRepository] UpdateAsync failed for {document.FirestoreId}: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string documentId)
    {
        try
        {
            await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.MedicalDocuments, documentId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentRepository] DeleteAsync failed for {documentId}: {ex.Message}");
            throw;
        }
    }

    public async Task<List<MedicalDocument>> GetByUserIdAsync(string userId)
    {
        try
        {
            var documents = await FirestoreService.Instance.QueryAsync(
                AppConfig.Collections.MedicalDocuments,
                whereField: "userId",
                whereValue: userId);

            return documents
                .Select(d => MapFromFirestore(d.Id, d.Fields))
                .OrderByDescending(d => d.UploadedAt)
                .ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentRepository] GetByUserIdAsync failed for {userId}: {ex.Message}");
            return new List<MedicalDocument>();
        }
    }

    private static MedicalDocument MapFromFirestore(string id, System.Text.Json.JsonElement fields)
    {
        return new MedicalDocument
        {
            FirestoreId = id,
            UserFirestoreId = FirestoreService.GetString(fields, "userId"),
            DocumentType = FirestoreService.GetString(fields, "documentType"),
            FileName = FirestoreService.GetString(fields, "fileName"),
            FilePath = FirestoreService.GetString(fields, "filePath"),
            StorageUrl = FirestoreService.GetString(fields, "storageUrl"),
            StoragePath = FirestoreService.GetString(fields, "storagePath"),
            Notes = FirestoreService.GetString(fields, "notes"),
            UploadedAt = FirestoreService.GetDateTime(fields, "uploadedAt"),
            FileSizeBytes = FirestoreService.GetInt(fields, "fileSizeBytes"),
            MimeType = FirestoreService.GetString(fields, "mimeType")
        };
    }

    private static Dictionary<string, object> MapToFirestore(MedicalDocument document) => new()
    {
        { "userId", document.UserFirestoreId },
        { "documentType", document.DocumentType },
        { "fileName", document.FileName },
        { "filePath", document.FilePath },
        { "storageUrl", document.StorageUrl ?? "" },
        { "storagePath", document.StoragePath ?? "" },
        { "notes", document.Notes },
        { "uploadedAt", document.UploadedAt },
        { "fileSizeBytes", document.FileSizeBytes },
        { "mimeType", document.MimeType ?? "" },
        { "updatedAt", DateTime.UtcNow }
    };
}
