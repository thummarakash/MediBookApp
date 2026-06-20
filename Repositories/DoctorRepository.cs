using MediBook.Configuration;
using MediBook.Extensions;
using MediBook.Models;
using MediBook.Services.Firebase;

namespace MediBook.Repositories;

public class DoctorRepository
{
    public static DoctorRepository Instance { get; } = new();
    private DoctorRepository() { }

    public async Task<List<Doctor>> GetAllAsync()
    {
        try
        {
            var collection_docs = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Doctors);
            return collection_docs.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepo] GetAllAsync query failed: {read_ex.Message}");
            return new List<Doctor>();
        }
    }

    public async Task<Doctor?> GetByIdAsync(string doctorId)
    {
        try
        {
            var doc_snap = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Doctors, doctorId);
            if (doc_snap == null) return null;
            var deserialized_fields = doc_snap.Value.TryGetProperty("fields", out var f) ? f : default;
            return MapFromFirestore(doctorId, deserialized_fields);
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepo] GetByIdAsync query failed for {doctorId}: {read_ex.Message}");
            return null;
        }
    }

    public async Task<string> CreateAsync(Doctor doctor_obj)
    {
        try
        {
            var deserialized_fields = MapToFirestore(doctor_obj);
            var inserted_doc_id = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Doctors, deserialized_fields);
            doctor_obj.FirestoreId = inserted_doc_id;
            return inserted_doc_id;
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepo] CreateAsync error: {fire_ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(Doctor doctor_obj)
    {
        try
        {
            if (string.IsNullOrEmpty(doctor_obj.FirestoreId)) return;
            var deserialized_fields = MapToFirestore(doctor_obj);
            await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Doctors, doctor_obj.FirestoreId, deserialized_fields);
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepo] UpdateAsync error: {fire_ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string doctorId)
    {
        try
        {
            await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Doctors, doctorId);
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepo] DeleteAsync failed for {doctorId}: {fire_ex.Message}");
            throw;
        }
    }

    public async Task SeedIfEmptyAsync(List<Doctor> seedData)
    {
        try
        {
            var doctors_list = await GetAllAsync();
            if (doctors_list.Count > 0) return;

            foreach (var doctor_obj in seedData)
            {
                var inserted_doc_id = await CreateAsync(doctor_obj);
                doctor_obj.FirestoreId = inserted_doc_id;
            }
        }
        catch (Exception seed_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepo] SeedIfEmptyAsync failed: {seed_ex.Message}");
        }
    }

    private static Doctor MapFromFirestore(string id, System.Text.Json.JsonElement fields)
    {
        return new Doctor
        {
            FirestoreId = id,
            Name = FirestoreService.GetString(fields, "name"),
            Specialty = FirestoreService.GetString(fields, "specialty"),
            Department = FirestoreService.GetString(fields, "department"),
            Availability = FirestoreService.GetString(fields, "availability"),
            Experience = FirestoreService.GetString(fields, "experience"),
            Rating = FirestoreService.GetString(fields, "rating"),
            Bio = FirestoreService.GetString(fields, "bio"),
            AccentColor = FirestoreService.GetString(fields, "accentColor").IfEmpty("#155EEF"),
            FeePerAppointment = FirestoreService.GetDouble(fields, "feePerAppointment"),
            FeePerMinute = FirestoreService.GetDouble(fields, "feePerMinute"),
            SlotDurationMinutes = FirestoreService.GetInt(fields, "slotDurationMinutes", 20),
            IsActive = FirestoreService.GetBool(fields, "isActive", true),
            ClinicName = FirestoreService.GetString(fields, "clinicName"),
            ClinicFirestoreId = FirestoreService.GetString(fields, "clinicFirestoreId")
        };
    }

    private static Dictionary<string, object> MapToFirestore(Doctor d) => new()
    {
        { "name", d.Name },
        { "specialty", d.Specialty },
        { "department", d.Department },
        { "availability", d.Availability },
        { "experience", d.Experience },
        { "rating", d.Rating },
        { "bio", d.Bio },
        { "accentColor", d.AccentColor },
        { "feePerAppointment", d.FeePerAppointment },
        { "feePerMinute", d.FeePerMinute },
        { "slotDurationMinutes", d.SlotDurationMinutes },
        { "isActive", d.IsActive },
        { "clinicName", d.ClinicName ?? "" },
        { "clinicFirestoreId", d.ClinicFirestoreId ?? "" },
        { "updatedAt", DateTime.UtcNow }
    };
}
