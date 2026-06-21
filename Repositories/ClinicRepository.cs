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
            var documents = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Clinics);
            return documents.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepository] GetAllAsync failed: {ex.Message}");
            return new List<Clinic>();
        }
    }

    public async Task<Clinic?> GetByIdAsync(string clinicId)
    {
        try
        {
            var snapshot = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Clinics, clinicId);
            if (snapshot == null) return null;
            var fields = snapshot.Value.TryGetProperty("fields", out var f) ? f : default;
            return MapFromFirestore(clinicId, fields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepository] GetByIdAsync failed for {clinicId}: {ex.Message}");
            return null;
        }
    }

    public async Task<string> CreateAsync(Clinic clinic)
    {
        try
        {
            var firestoreFields = MapToFirestore(clinic);
            var newDocId = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Clinics, firestoreFields);
            clinic.FirestoreId = newDocId;
            return newDocId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepository] CreateAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(Clinic clinic)
    {
        try
        {
            if (string.IsNullOrEmpty(clinic.FirestoreId)) return;
            var firestoreFields = MapToFirestore(clinic);
            await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Clinics, clinic.FirestoreId, firestoreFields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepository] UpdateAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string clinicId)
    {
        try
        {
            await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Clinics, clinicId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepository] DeleteAsync failed for {clinicId}: {ex.Message}");
            throw;
        }
    }

    public async Task SeedIfEmptyAsync(List<Clinic> seedData)
    {
        try
        {
            var existing = await GetAllAsync();
            if (existing.Count > 0) return;

            foreach (var clinic in seedData)
            {
                var newDocId = await CreateAsync(clinic);
                clinic.FirestoreId = newDocId;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ClinicRepository] SeedIfEmptyAsync failed: {ex.Message}");
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

    private static Dictionary<string, object> MapToFirestore(Clinic clinic) => new()
    {
        { "name", clinic.Name },
        { "address", clinic.Address },
        { "latitude", clinic.Latitude },
        { "longitude", clinic.Longitude },
        { "phone", clinic.Phone ?? "" },
        { "openingHoursMonFri", clinic.OpeningHoursMonFri ?? "Mon-Fri: 8:00 AM - 6:00 PM" },
        { "openingHoursSatSun", clinic.OpeningHoursSatSun ?? "Sat: 9:00 AM - 1:00 PM" },
        { "status", clinic.Status ?? "Open" },
        { "updatedAt", DateTime.UtcNow }
    };
}
