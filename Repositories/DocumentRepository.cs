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
        var fields = MapToFirestore(document);
        var docId = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.MedicalDocuments, fields);
        document.FirestoreId = docId;
        return docId;
    }

    public async Task UpdateAsync(MedicalDocument document)
    {
        if (string.IsNullOrEmpty(document.FirestoreId)) return;
        await FirestoreService.Instance.UpdateDocumentAsync(
            AppConfig.Collections.MedicalDocuments,
            document.FirestoreId,
            MapToFirestore(document));
    }

    public async Task DeleteAsync(string documentId)
        => await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.MedicalDocuments, documentId);

    public async Task<List<MedicalDocument>> GetByUserIdAsync(string userId)
    {
        var docs = await FirestoreService.Instance.QueryAsync(
            AppConfig.Collections.MedicalDocuments,
            whereField: "userId",
            whereValue: userId,
            orderByField: "uploadedAt",
            descending: true);

        return docs.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
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
