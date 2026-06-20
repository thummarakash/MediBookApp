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
        try
        {
            var collection_docs = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Clinics);
            return collection_docs.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepo] GetAllAsync query failed: {read_ex.Message}");
            return new List<Clinic>();
        }
    }

    public async Task<Clinic?> GetByIdAsync(string clinicId)
    {
        try
        {
            var doc_item = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Clinics, clinicId);
            if (doc_item == null) return null;
            var deserialized_fields = doc_item.Value.TryGetProperty("fields", out var f) ? f : default;
            return MapFromFirestore(clinicId, deserialized_fields);
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepo] GetByIdAsync query failed for {clinicId}: {read_ex.Message}");
            return null;
        }
    }

    public async Task<string> CreateAsync(Clinic clinic)
    {
        try
        {
            var deserialized_fields = MapToFirestore(clinic);
            var db_id = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Clinics, deserialized_fields);
            clinic.FirestoreId = db_id;
            return db_id;
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepo] CreateAsync error: {fire_ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(Clinic clinic)
    {
        try
        {
            if (string.IsNullOrEmpty(clinic.FirestoreId)) return;
            var deserialized_fields = MapToFirestore(clinic);
            await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Clinics, clinic.FirestoreId, deserialized_fields);
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepo] UpdateAsync error: {fire_ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string clinicId)
    {
        try
        {
            await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Clinics, clinicId);
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepo] DeleteAsync error for {clinicId}: {fire_ex.Message}");
            throw;
        }
    }

    public async Task SeedIfEmptyAsync(List<Clinic> seedData)
    {
        try
        {
            var clinics_list = await GetAllAsync();
            if (clinics_list.Count > 0) return;

            foreach (var clinic in seedData)
            {
                var db_id = await CreateAsync(clinic);
                clinic.FirestoreId = db_id;
            }
        }
        catch (Exception seed_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepo] SeedIfEmptyAsync error: {seed_ex.Message}");
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

    private static Dictionary<string, object> MapToFirestore(Clinic cl) => new()
    {
        { "name", cl.Name },
        { "address", cl.Address },
        { "latitude", cl.Latitude },
        { "longitude", cl.Longitude },
        { "phone", cl.Phone ?? "" },
        { "openingHoursMonFri", cl.OpeningHoursMonFri ?? "Mon-Fri: 8:00 AM - 6:00 PM" },
        { "openingHoursSatSun", cl.OpeningHoursSatSun ?? "Sat: 9:00 AM - 1:00 PM" },
        { "status", cl.Status ?? "Open" },
        { "updatedAt", DateTime.UtcNow }
    };
}
