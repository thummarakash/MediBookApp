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
        var fields = MapToFirestore(appointment);
        var docId = await FirestoreService.Instance.AddDocumentAsync(AppConfig.Collections.Appointments, fields);
        appointment.FirestoreId = docId;
        return docId;
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        if (string.IsNullOrEmpty(appointment.FirestoreId)) return;
        var fields = MapToFirestore(appointment);
        await FirestoreService.Instance.UpdateDocumentAsync(
            AppConfig.Collections.Appointments, appointment.FirestoreId, fields);
    }

    public async Task UpdateStatusAsync(string appointmentId, string status)
    {
        await FirestoreService.Instance.UpdateDocumentAsync(
            AppConfig.Collections.Appointments, appointmentId,
            new Dictionary<string, object> { { "status", status }, { "updatedAt", DateTime.UtcNow } });
    }

    public async Task DeleteAsync(string appointmentId)
        => await FirestoreService.Instance.DeleteDocumentAsync(AppConfig.Collections.Appointments, appointmentId);

    public async Task<Appointment?> GetByIdAsync(string appointmentId)
    {
        var doc = await FirestoreService.Instance.GetDocumentAsync(AppConfig.Collections.Appointments, appointmentId);
        if (doc == null) return null;
        var fields = doc.Value.TryGetProperty("fields", out var f) ? f : default;
        return MapFromFirestore(appointmentId, fields);
    }

    public async Task<List<Appointment>> GetByUserIdAsync(string userId)
    {
        var docs = await FirestoreService.Instance.QueryAsync(
            AppConfig.Collections.Appointments,
            whereField: "userId",
            whereValue: userId,
            orderByField: "createdAt",
            descending: true);

        return docs.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
    }

    public async Task<List<Appointment>> GetAllAsync()
    {
        var docs = await FirestoreService.Instance.GetCollectionAsync(AppConfig.Collections.Appointments, limit: 200);
        return docs.Select(d => MapFromFirestore(d.Id, d.Fields)).ToList();
    }

    public async Task<Appointment?> GetNextUpcomingAsync(string userId)
    {
        var all = await GetByUserIdAsync(userId);
        return all
            .Where(a => a.Status == "Upcoming")
            .OrderBy(a => a.FullDateTime)
            .FirstOrDefault();
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
