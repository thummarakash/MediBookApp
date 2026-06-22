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
            var documents = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Doctors);
            return documents.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepository] GetAllAsync failed: {ex.Message}");
            return new List<Doctor>();
        }
    }

    public async Task<Doctor?> GetByIdAsync(string doctorId)
    {
        try
        {
            var snapshot = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Doctors, doctorId);
            if (snapshot == null) return null;
            var fields = snapshot.Value.TryGetProperty("fields", out var f) ? f : default;
            return MapFromFirestore(doctorId, fields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepository] GetByIdAsync failed for {doctorId}: {ex.Message}");
            return null;
        }
    }

    public async Task<string> CreateAsync(Doctor doctor)
    {
        try
        {
            var firestoreFields = MapToFirestore(doctor);
            var newDocId = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Doctors, firestoreFields);
            doctor.FirestoreId = newDocId;
            return newDocId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepository] CreateAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        try
        {
            if (string.IsNullOrEmpty(doctor.FirestoreId)) return;
            var firestoreFields = MapToFirestore(doctor);
            await FirestoreService.Instance.SetDocumentAsync(AppConfig.Collections.Doctors, doctor.FirestoreId, firestoreFields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepository] UpdateAsync failed: {ex.Message}");
            throw;
        }
    }

    public async Task DeleteAsync(string doctorId)
    {
        try
        {
            await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Doctors, doctorId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepository] DeleteAsync failed for {doctorId}: {ex.Message}");
            throw;
        }
    }

    public async Task SeedIfEmptyAsync(List<Doctor> seedData)
    {
        try
        {
            var existing = await GetAllAsync();
            if (existing.Count > 0) return;

            foreach (var doctor in seedData)
            {
                var newDocId = await CreateAsync(doctor);
                doctor.FirestoreId = newDocId;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorRepository] SeedIfEmptyAsync failed: {ex.Message}");
        }
    }

    private static Doctor MapFromFirestore(string id, System.Text.Json.JsonElement fields)
    {
        return new Doctor
        {
            FirestoreId = id,
            Name = FirestoreService.GetString(fields, "name"),
            Email = FirestoreService.GetString(fields, "email"),
            Specialty = FirestoreService.GetString(fields, "specialty"),
            Department = FirestoreService.GetString(fields, "department"),
            FeePerAppointment = FirestoreService.GetDouble(fields, "feePerAppointment"),
            ClinicName = FirestoreService.GetString(fields, "clinicName"),
            ClinicFirestoreId = FirestoreService.GetString(fields, "clinicFirestoreId"),
            ScheduleJson = FirestoreService.GetString(fields, "scheduleJson")
        };
    }

    private static Dictionary<string, object> MapToFirestore(Doctor doctor)
    {
        var dict = new Dictionary<string, object>
        {
            { "name", doctor.Name },
            { "email", doctor.Email },
            { "specialty", doctor.Specialty },
            { "department", doctor.Department },
            { "feePerAppointment", doctor.FeePerAppointment }
        };
        if (!string.IsNullOrEmpty(doctor.ClinicName)) dict["clinicName"] = doctor.ClinicName;
        if (!string.IsNullOrEmpty(doctor.ClinicFirestoreId)) dict["clinicFirestoreId"] = doctor.ClinicFirestoreId;
        if (!string.IsNullOrEmpty(doctor.ScheduleJson)) dict["scheduleJson"] = doctor.ScheduleJson;
        return dict;
    }
}
