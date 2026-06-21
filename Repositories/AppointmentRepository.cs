using MediBook.Configuration;
using MediBook.Extensions;
using MediBook.Models;
using MediBook.Services.Firebase;

namespace MediBook.Repositories;

public class AppointmentRepository
{
    public static AppointmentRepository Instance { get; } = new();
    private AppointmentRepository() { }

    public async Task<string> CreateAsync(Appointment appointment)
    {
        try
        {
            var firestoreFields = MapToFirestore(appointment);
            var newDocId = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Appointments, firestoreFields);
            appointment.FirestoreId = newDocId;
            return newDocId;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptRepo] CreateAsync: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        try
        {
            if (string.IsNullOrEmpty(appointment.FirestoreId)) return;
            var firestoreFields = MapToFirestore(appointment);
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Appointments, appointment.FirestoreId, firestoreFields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptRepo] UpdateAsync: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateStatusAsync(string appointmentId, string status)
    {
        try
        {
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Appointments, appointmentId,
                new Dictionary<string, object> { { "status", status }, { "updatedAt", DateTime.UtcNow } });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptRepo] status update failed for {appointmentId}: {ex.Message}");
        }
    }

    public async Task DeleteAsync(string appointmentId)
    {
        try
        {
            await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Appointments, appointmentId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptRepo] DeleteAsync ({appointmentId}): {ex.Message}");
            throw;
        }
    }

    public async Task<Appointment?> GetByIdAsync(string appointmentId)
    {
        try
        {
            var snapshot = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Appointments, appointmentId);
            if (snapshot == null) return null;
            var fields = snapshot.Value.TryGetProperty("fields", out var f) ? f : default;
            return MapFromFirestore(appointmentId, fields);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptRepo] GetByIdAsync ({appointmentId}): {ex.Message}");
            return null;
        }
    }

    public async Task<List<Appointment>> GetByUserIdAsync(string userId)
    {
        try
        {
            var documents = await FirestoreService.Instance.QueryAsync(
                AppConfig.Collections.Appointments,
                whereField: "userId",
                whereValue: userId,
                orderByField: "createdAt",
                descending: true);

            return documents.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptRepo] GetByUserIdAsync failed for user {userId}: {ex.Message}");
            return new List<Appointment>();
        }
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        try
        {
            var documents = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Appointments, limit: 200);
            return documents.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptRepo] GetAllAsync: {ex.Message}");
            return new List<Appointment>();
        }
    }

    public async Task<Appointment?> GetNextUpcomingAsync(string userId)
    {
        try
        {
            var appointments = await GetByUserIdAsync(userId);
            return appointments
                .Where(a => a.Status == "Upcoming")
                .OrderBy(a => a.FullDateTime)
                .FirstOrDefault();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptRepo] GetNextUpcoming for {userId}: {ex.Message}");
            return null;
        }
    }

    private static Appointment MapFromFirestore(string id, System.Text.Json.JsonElement fields)
    {
        // UserId stays 0 after migration — only UserFirestoreId is meaningful now
        return new Appointment
        {
            FirestoreId = id,
            UserId = 0,
            UserFirestoreId = FirestoreService.GetString(fields, "userId"),
            DoctorId = FirestoreService.GetInt(fields, "doctorId"),
            DoctorFirestoreId = FirestoreService.GetString(fields, "doctorFirestoreId"),
            DoctorName = FirestoreService.GetString(fields, "doctorName"),
            Department = FirestoreService.GetString(fields, "department"),
            ClinicName = FirestoreService.GetString(fields, "clinicName"),
            ClinicFirestoreId = FirestoreService.GetString(fields, "clinicFirestoreId"),
            DateText = FirestoreService.GetString(fields, "dateText"),
            TimeText = FirestoreService.GetString(fields, "timeText"),
            Reason = FirestoreService.GetString(fields, "reason"),
            TotalFee = FirestoreService.GetDouble(fields, "totalFee"),
            Status = FirestoreService.GetString(fields, "status").IfEmpty("Upcoming"),
            ReminderEnabled = FirestoreService.GetBool(fields, "reminderEnabled", true),
            EmailReminderQueued = FirestoreService.GetBool(fields, "emailReminderQueued"),
            CreatedAt = FirestoreService.GetDateTime(fields, "createdAt"),
            UpdatedAt = FirestoreService.GetDateTime(fields, "updatedAt")
        };
    }

    private static Dictionary<string, object> MapToFirestore(Appointment appointment) => new()
    {
        { "userId", appointment.UserFirestoreId },
        { "doctorFirestoreId", appointment.DoctorFirestoreId },
        { "doctorId", appointment.DoctorId },
        { "doctorName", appointment.DoctorName },
        { "department", appointment.Department },
        { "clinicName", appointment.ClinicName },
        { "clinicFirestoreId", appointment.ClinicFirestoreId },
        { "dateText", appointment.DateText },
        { "timeText", appointment.TimeText },
        { "reason", appointment.Reason },
        { "totalFee", appointment.TotalFee },
        { "status", appointment.Status },
        { "reminderEnabled", appointment.ReminderEnabled },
        { "emailReminderQueued", appointment.EmailReminderQueued },
        { "createdAt", appointment.CreatedAt },
        { "updatedAt", DateTime.UtcNow }
    };
}
