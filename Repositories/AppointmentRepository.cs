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
            var db_fields = MapToFirestore(appointment);
            var inserted_doc_id = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Appointments, db_fields);
            appointment.FirestoreId = inserted_doc_id;
            return inserted_doc_id;
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppointmentRepo] CreateAsync error: {fire_ex.Message}");
            throw;
        }
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        try
        {
            if (string.IsNullOrEmpty(appointment.FirestoreId)) return;
            var db_fields = MapToFirestore(appointment);
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Appointments, appointment.FirestoreId, db_fields);
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppointmentRepo] UpdateAsync error: {fire_ex.Message}");
            throw;
        }
    }

    public async Task UpdateStatusAsync(string appointmentId, string status)
    {
        try
        {
            // Updating only status and updatedAt fields directly in firestore
            await FirestoreService.Instance.UpdateDocumentAsync(
                AppConfig.Collections.Appointments, appointmentId,
                new Dictionary<string, object> { { "status", status }, { "updatedAt", DateTime.UtcNow } });
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppointmentRepo] UpdateStatusAsync error for {appointmentId}: {fire_ex.Message}");
        }
    }

    public async Task DeleteAsync(string appointmentId)
    {
        try
        {
            await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Appointments, appointmentId);
        }
        catch (Exception fire_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppointmentRepo] DeleteAsync error for {appointmentId}: {fire_ex.Message}");
            throw;
        }
    }

    public async Task<Appointment?> GetByIdAsync(string appointmentId)
    {
        try
        {
            var query_snapshot = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Appointments, appointmentId);
            if (query_snapshot == null) return null;
            var deserialized_fields = query_snapshot.Value.TryGetProperty("fields", out var f) ? f : default;
            return MapFromFirestore(appointmentId, deserialized_fields);
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppointmentRepo] GetByIdAsync query failed for {appointmentId}: {read_ex.Message}");
            return null;
        }
    }

    public async Task<List<Appointment>> GetByUserIdAsync(string userId)
    {
        try
        {
            var doc_list = await FirestoreService.Instance.QueryAsync(
                AppConfig.Collections.Appointments,
                whereField: "userId",
                whereValue: userId,
                orderByField: "createdAt",
                descending: true);

            return doc_list.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppointmentRepo] GetByUserIdAsync query error for {userId}: {read_ex.Message}");
            return new List<Appointment>();
        }
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        try
        {
            var doc_list = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Appointments, limit: 200);
            return doc_list.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppointmentRepo] GetAllAsync query failed: {read_ex.Message}");
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
        catch (Exception query_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppointmentRepo] GetNextUpcomingAsync error for {userId}: {query_ex.Message}");
            return null;
        }
    }

    private static Appointment MapFromFirestore(string id, System.Text.Json.JsonElement fields)
    {
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

    private static Dictionary<string, object> MapToFirestore(Appointment a) => new()
    {
        { "userId", a.UserFirestoreId },
        { "doctorFirestoreId", a.DoctorFirestoreId },
        { "doctorId", a.DoctorId },
        { "doctorName", a.DoctorName },
        { "department", a.Department },
        { "clinicName", a.ClinicName },
        { "clinicFirestoreId", a.ClinicFirestoreId },
        { "dateText", a.DateText },
        { "timeText", a.TimeText },
        { "reason", a.Reason },
        { "totalFee", a.TotalFee },
        { "status", a.Status },
        { "reminderEnabled", a.ReminderEnabled },
        { "emailReminderQueued", a.EmailReminderQueued },
        { "createdAt", a.CreatedAt },
        { "updatedAt", DateTime.UtcNow }
    };
}
