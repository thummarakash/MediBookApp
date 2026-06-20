using MediBook.Models;
using SQLite;

namespace MediBook.Services.Offline;

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
        try
        {
            // Initializing local sqlite db context
            _db = new SQLiteAsyncConnection(DbPath,
                SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);

            await _db.CreateTableAsync<CachedClinic>();
            await _db.CreateTableAsync<CachedDoctor>();
            await _db.CreateTableAsync<CachedAppointment>();
            return _db;
        }
        catch (Exception conn_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalCache] Failed to build or open sqlite DB: {conn_ex.Message}");
            throw;
        }
    }

    public async Task CacheClinicsAsync(List<Clinic> clin_list)
    {
        try
        {
            var sqlite_conn = await GetConnectionAsync();
            await sqlite_conn.DeleteAllAsync<CachedClinic>();
            
            // Map model list to cached db structure
            var entities_to_insert = clin_list.Select(c => new CachedClinic
            {
                FirestoreId = c.FirestoreId,
                Name = c.Name,
                Address = c.Address,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Phone = c.Phone ?? string.Empty,
                CachedAt = DateTime.UtcNow
            }).ToList();
            
            await sqlite_conn.InsertAllAsync(entities_to_insert);
        }
        catch (Exception cache_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalCache] CacheClinicsAsync failed: {cache_ex.Message}");
        }
    }

    public async Task<List<Clinic>> GetCachedClinicsAsync()
    {
        try
        {
            var sqlite_conn = await GetConnectionAsync();
            var cached_rows = await sqlite_conn.Table<CachedClinic>().ToListAsync();
            
            return cached_rows.Select(c => new Clinic
            {
                FirestoreId = c.FirestoreId,
                Name = c.Name,
                Address = c.Address,
                Latitude = c.Latitude,
                Longitude = c.Longitude,
                Phone = c.Phone
            }).ToList();
        }
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalCache] GetCachedClinicsAsync query failed: {read_ex.Message}");
            return new List<Clinic>();
        }
    }

    public async Task CacheDoctorsAsync(List<Doctor> doc_list)
    {
        try
        {
            var sqlite_conn = await GetConnectionAsync();
            await sqlite_conn.DeleteAllAsync<CachedDoctor>();
            
            var entities_to_insert = doc_list.Select(d => new CachedDoctor
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
            
            await sqlite_conn.InsertAllAsync(entities_to_insert);
        }
        catch (Exception cache_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalCache] CacheDoctorsAsync failed: {cache_ex.Message}");
        }
    }

    public async Task<List<Doctor>> GetCachedDoctorsAsync()
    {
        try
        {
            var sqlite_conn = await GetConnectionAsync();
            var cached_rows = await sqlite_conn.Table<CachedDoctor>().ToListAsync();
            
            return cached_rows.Select(d => new Doctor
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
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalCache] GetCachedDoctorsAsync query failed: {read_ex.Message}");
            return new List<Doctor>();
        }
    }

    public async Task CacheAppointmentsAsync(string user_uid, List<Appointment> appointment_list)
    {
        try
        {
            var sqlite_conn = await GetConnectionAsync();
            await sqlite_conn.ExecuteAsync("DELETE FROM CachedAppointment WHERE UserId = ?", user_uid);
            
            var entities_to_insert = appointment_list.Select(a => new CachedAppointment
            {
                FirestoreId = a.FirestoreId,
                UserId = user_uid,
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
            
            await sqlite_conn.InsertAllAsync(entities_to_insert);
        }
        catch (Exception cache_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalCache] CacheAppointmentsAsync failed for user {user_uid}: {cache_ex.Message}");
        }
    }

    public async Task<List<Appointment>> GetCachedAppointmentsAsync(string user_uid)
    {
        try
        {
            var sqlite_conn = await GetConnectionAsync();
            var cached_rows = await sqlite_conn.Table<CachedAppointment>()
                .Where(a => a.UserId == user_uid)
                .ToListAsync();

            return cached_rows.Select(a => new Appointment
            {
                FirestoreId = a.FirestoreId,
                UserFirestoreId = user_uid,
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
        catch (Exception read_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocalCache] GetCachedAppointmentsAsync failed for user {user_uid}: {read_ex.Message}");
            return new List<Appointment>();
        }
    }
}

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
