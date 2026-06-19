using MediBook.Models;
using SQLite;

namespace MediBook.Services.Offline;

/// <summary>
/// SQLite-backed local cache. Used as offline fallback for clinics and doctors
/// so the app is usable without an internet connection.
/// </summary>
public class LocalCacheService
{
    private static readonly string DbPath = Path.Combine(
        FileSystem.AppDataDirectory, "medibook_cache.db3");

    private SQLiteAsyncConnection? _db;

    public static LocalCacheService Instance { get; } = new();
    private LocalCacheService() { }

    private async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        if (_db != null) return _db;
        _db = new SQLiteAsyncConnection(DbPath,
            SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

        await _db.CreateTableAsync<CachedClinic>();
        await _db.CreateTableAsync<CachedDoctor>();
        await _db.CreateTableAsync<CachedAppointment>();
        return _db;
    }

    // ── Clinics ───────────────────────────────────────────────────────────────

    public async Task CacheClinicsAsync(List<Clinic> clinics)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAllAsync<CachedClinic>();
        var cached = clinics.Select(c => new CachedClinic
        {
            FirestoreId = c.FirestoreId,
            Name = c.Name,
            Address = c.Address,
            Latitude = c.Latitude,
            Longitude = c.Longitude,
            Phone = c.Phone ?? string.Empty,
            CachedAt = DateTime.UtcNow
        }).ToList();
        await db.InsertAllAsync(cached);
    }

    public async Task<List<Clinic>> GetCachedClinicsAsync()
    {
        var db = await GetConnectionAsync();
        var cached = await db.Table<CachedClinic>().ToListAsync();
        return cached.Select(c => new Clinic
        {
            FirestoreId = c.FirestoreId,
            Name = c.Name,
            Address = c.Address,
            Latitude = c.Latitude,
            Longitude = c.Longitude,
            Phone = c.Phone
        }).ToList();
    }

    // ── Doctors ───────────────────────────────────────────────────────────────

    public async Task CacheDoctorsAsync(List<Doctor> doctors)
    {
        var db = await GetConnectionAsync();
        await db.DeleteAllAsync<CachedDoctor>();
        var cached = doctors.Select(d => new CachedDoctor
        {
            FirestoreId = d.FirestoreId,
            Name = d.Name,
            Specialty = d.Specialty,
            Department = d.Department,
            Availability = d.Availability,
            Experience = d.Experience,
            Rating = d.Rating,
            FeePerAppointment = d.FeePerAppointment,
            ClinicName = d.ClinicName,
            AccentColor = d.AccentColor,
            CachedAt = DateTime.UtcNow
        }).ToList();
        await db.InsertAllAsync(cached);
    }

    public async Task<List<Doctor>> GetCachedDoctorsAsync()
    {
        var db = await GetConnectionAsync();
        var cached = await db.Table<CachedDoctor>().ToListAsync();
        return cached.Select(d => new Doctor
        {
            FirestoreId = d.FirestoreId,
            Name = d.Name,
            Specialty = d.Specialty,
            Department = d.Department,
            Availability = d.Availability,
            Experience = d.Experience,
            Rating = d.Rating,
            FeePerAppointment = d.FeePerAppointment,
            ClinicName = d.ClinicName,
            AccentColor = d.AccentColor
        }).ToList();
    }

    // ── Appointments ──────────────────────────────────────────────────────────

    public async Task CacheAppointmentsAsync(string userId, List<Appointment> appointments)
    {
        var db = await GetConnectionAsync();
        await db.ExecuteAsync("DELETE FROM CachedAppointment WHERE UserId = ?", userId);
        var cached = appointments.Select(a => new CachedAppointment
        {
            FirestoreId = a.FirestoreId,
            UserId = userId,
            DoctorName = a.DoctorName,
            Department = a.Department,
            ClinicName = a.ClinicName,
            DateText = a.DateText,
            TimeText = a.TimeText,
            Status = a.Status,
            TotalFee = a.TotalFee,
            Reason = a.Reason,
            CachedAt = DateTime.UtcNow
        }).ToList();
        await db.InsertAllAsync(cached);
    }

    public async Task<List<Appointment>> GetCachedAppointmentsAsync(string userId)
    {
        var db = await GetConnectionAsync();
        var cached = await db.Table<CachedAppointment>()
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return cached.Select(a => new Appointment
        {
            FirestoreId = a.FirestoreId,
            UserFirestoreId = userId,
            DoctorName = a.DoctorName,
            Department = a.Department,
            ClinicName = a.ClinicName,
            DateText = a.DateText,
            TimeText = a.TimeText,
            Status = a.Status,
            TotalFee = a.TotalFee,
            Reason = a.Reason
        }).ToList();
    }
}

// ── SQLite entity classes ─────────────────────────────────────────────────────

[Table("CachedClinic")]
public class CachedClinic
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string FirestoreId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Phone { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}

[Table("CachedDoctor")]
public class CachedDoctor
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string FirestoreId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Availability { get; set; } = string.Empty;
    public string Experience { get; set; } = string.Empty;
    public string Rating { get; set; } = string.Empty;
    public double FeePerAppointment { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#155EEF";
    public DateTime CachedAt { get; set; }
}

[Table("CachedAppointment")]
public class CachedAppointment
{
    [PrimaryKey, AutoIncrement] public int Id { get; set; }
    public string FirestoreId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string ClinicName { get; set; } = string.Empty;
    public string DateText { get; set; } = string.Empty;
    public string TimeText { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double TotalFee { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
}
