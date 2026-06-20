using MediBook.Configuration;
using MediBook.Models;
using MediBook.Services.Firebase;

namespace MediBook.Repositories;

public class DocumentRepository
{
    public static DocumentRepository Instance { get; } = new();
    private DocumentRepository() { }

    public async Task<string> CreateAsync(MedicalDocument doc_obj)
    {
        try
        {
            var deserialized_fields = MapToFirestore(doc_obj);
            var inserted_doc_id = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.MedicalDocuments, deserialized_fields);
            doc_obj.FirestoreId = inserted_doc_id;
            return inserted_doc_id;
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentRepo] CreateAsync error: {fire_ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(MedicalDocument doc_obj)
    {
        try
        {
            if (string.IsNullOrEmpty(doc_obj.FirestoreId)) return;
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.MedicalDocuments,
                doc_obj.FirestoreId,
                MapToFirestore(doc_obj));
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentRepo] UpdateAsync error for {doc_obj.FirestoreId}: {fire_ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string documentId)
    {
        try
        {
            await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.MedicalDocuments, documentId);
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentRepo] DeleteAsync failed for {documentId}: {fire_ex.Message}");
            throw;
        }
    }

    public async Task<List<MedicalDocument>> GetByUserIdAsync(string userId)
    {
        try
        {
            var collection_docs = await FirestoreService.Instance.QueryAsync(
                AppConfig.Collections.MedicalDocuments,
                whereField: "userId",
                whereValue: userId,
                orderByField: "uploadedAt",
                descending: true);

            return collection_docs.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DocumentRepo] GetByUserIdAsync query failed for user {userId}: {read_ex.Message}");
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

    private static Dictionary<string, object> MapToFirestore(MedicalDocument d) => new()
    {
        { "userId", d.UserFirestoreId },
        { "documentType", d.DocumentType },
        { "fileName", d.FileName },
        { "filePath", d.FilePath },
        { "storageUrl", d.StorageUrl ?? "" },
        { "storagePath", d.StoragePath ?? "" },
        { "notes", d.Notes },
        { "uploadedAt", d.UploadedAt },
        { "fileSizeBytes", d.FileSizeBytes },
        { "mimeType", d.MimeType ?? "" },
        { "updatedAt", DateTime.UtcNow }
    };
}
