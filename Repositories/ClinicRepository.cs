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
            ScheduleJson = FirestoreService.GetString(fields, "scheduleJson")
        };
    }

    private static Dictionary<string, object> MapToFirestore(Clinic clinic)
    {
        var dict = new Dictionary<string, object>
        {
            { "name", clinic.Name },
            { "address", clinic.Address },
            { "latitude", clinic.Latitude },
            { "longitude", clinic.Longitude }
        };
        if (!string.IsNullOrEmpty(clinic.ScheduleJson)) dict["scheduleJson"] = clinic.ScheduleJson;
        return dict;
    }
}
