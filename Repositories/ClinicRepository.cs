using MediBook.Configuration;
using MediBook.Extensions;
using MediBook.Models;
using MediBook.Services.Firebase;

namespace MediBook.Repositories;

public class ClinicRepository
{
    public static ClinicRepository Instance { get; } = new();
    private ClinicRepository() { }

    public async Task<List<Clinic>> GetAllAsync()
    {
        var docs = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Clinics);
        return docs.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
    }

    public async Task<Clinic?> GetByIdAsync(string clinicId)
    {
        var doc = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Clinics, clinicId);
        if (doc == null) return null;
        var fields = doc.Value.TryGetProperty("fields", out var f) ? f : default;
        return MapFromFirestore(clinicId, fields);
    }

    public async Task<string> CreateAsync(Clinic clinic)
    {
        var fields = MapToFirestore(clinic);
        var docId = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Clinics, fields);
        clinic.FirestoreId = docId;
        return docId;
    }

    public async Task UpdateAsync(Clinic clinic)
    {
        if (string.IsNullOrEmpty(clinic.FirestoreId)) return;
        var fields = MapToFirestore(clinic);
        await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Clinics, clinic.FirestoreId, fields);
    }

    public async Task DeleteAsync(string clinicId)
        => await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Clinics, clinicId);

    // Seed initial clinic data if Firestore collection is empty
    public async Task SeedIfEmptyAsync(List<Clinic> seedData)
    {
        var existing = await GetAllAsync();
        if (existing.Count > 0) return;

        foreach (var clinic in seedData)
        {
            var docId = await CreateAsync(clinic);
            clinic.FirestoreId = docId;
        }
    }

    private static Clinic MapFromFirestore(string id, System.Text.Json.JsonElement fields)
    {
        return new Clinic
        {
            FirestoreId = id,
            Name = FirestoreService.GetString(fields, "name"),
            Address = FirestoreService.GetString(fields, "address"),
            Latitude = FirestoreService.GetDouble(fields, "latitude"),
            Longitude = FirestoreService.GetDouble(fields, "longitude"),
            Phone = FirestoreService.GetString(fields, "phone"),
            OpeningHoursMonFri = FirestoreService.GetString(fields, "openingHoursMonFri"),
            OpeningHoursSatSun = FirestoreService.GetString(fields, "openingHoursSatSun"),
            Status = FirestoreService.GetString(fields, "status").IfEmpty("Open")
        };
    }

    private static Dictionary<string, object> MapToFirestore(Clinic c) => new()
    {
        { "name", c.Name },
        { "address", c.Address },
        { "latitude", c.Latitude },
        { "longitude", c.Longitude },
        { "phone", c.Phone ?? "" },
        { "openingHoursMonFri", c.OpeningHoursMonFri ?? "Mon-Fri: 8:00 AM - 6:00 PM" },
        { "openingHoursSatSun", c.OpeningHoursSatSun ?? "Sat: 9:00 AM - 1:00 PM" },
        { "status", c.Status ?? "Open" },
        { "updatedAt", DateTime.UtcNow }
    };
}
