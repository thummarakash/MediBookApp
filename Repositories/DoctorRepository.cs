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
        var docs = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Doctors);
        return docs.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
    }

    public async Task<Doctor?> GetByIdAsync(string doctorId)
    {
        var doc = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Doctors, doctorId);
        if (doc == null) return null;
        var fields = doc.Value.TryGetProperty("fields", out var f) ? f : default;
        return MapFromFirestore(doctorId, fields);
    }

    public async Task<string> CreateAsync(Doctor doctor)
    {
        var fields = MapToFirestore(doctor);
        var docId = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Doctors, fields);
        doctor.FirestoreId = docId;
        return docId;
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        if (string.IsNullOrEmpty(doctor.FirestoreId)) return;
        var fields = MapToFirestore(doctor);
        await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Doctors, doctor.FirestoreId, fields);
    }

    public async Task DeleteAsync(string doctorId)
        => await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Doctors, doctorId);

    public async Task SeedIfEmptyAsync(List<Doctor> seedData)
    {
        var existing = await GetAllAsync();
        if (existing.Count > 0) return;

        foreach (var doctor in seedData)
        {
            var docId = await CreateAsync(doctor);
            doctor.FirestoreId = docId;
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
