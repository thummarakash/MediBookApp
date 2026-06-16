using System.Security.Cryptography;
using System.Text;
using Microsoft.Maui.Storage;
using SQLite;
using MediBook.Models;

namespace MediBook.Services;

public class DatabaseService
{
    private const string CurrentUserKey = "medibook_current_user_id";
    private SQLiteAsyncConnection? _database;
    public static DatabaseService Instance { get; } = new();

    private DatabaseService()
    {
    }

    private async Task InitAsync()
    {
        if (_database != null)
        {
            return;
        }

        SQLitePCL.Batteries_V2.Init();
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "medibook.db3");
        _database = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
        await _database.CreateTableAsync<UserAccount>();
        await _database.CreateTableAsync<Doctor>();
        await _database.CreateTableAsync<Appointment>();
        await _database.CreateTableAsync<MedicalDocument>();
        await _database.CreateTableAsync<EmailReminder>();
        await SeedDoctorsAsync();
    }

    public async Task SeedDoctorsAsync()
    {
        await InitWithoutSeedAsync();
        if (_database == null)
        {
            return;
        }

        var doctorCount = await _database.Table<Doctor>().CountAsync();
        if (doctorCount > 0)
        {
            return;
        }

        var doctors = new List<Doctor>
        {
            new() { Name = "Dr Emily Carter", Specialty = "General Practitioner", Department = "General Care", Availability = "Today • 9:00 AM - 4:00 PM", Experience = "9 years", Rating = "4.9", Bio = "General health checks, prescriptions, referrals and family medicine.", AccentColor = "#155EEF" },
            new() { Name = "Dr Noah Williams", Specialty = "Cardiologist", Department = "Heart Clinic", Availability = "Tomorrow • 10:00 AM - 2:00 PM", Experience = "12 years", Rating = "4.8", Bio = "Heart health screening, chest pain review and blood pressure care.", AccentColor = "#EF4444" },
            new() { Name = "Dr Olivia Brown", Specialty = "Dermatologist", Department = "Skin Care", Availability = "Wed • 11:30 AM - 5:00 PM", Experience = "7 years", Rating = "4.7", Bio = "Skin checks, allergy review, acne treatment and mole assessments.", AccentColor = "#2DD4BF" },
            new() { Name = "Dr Lucas Martin", Specialty = "Physiotherapist", Department = "Physiotherapy", Availability = "Thu • 8:30 AM - 3:30 PM", Experience = "8 years", Rating = "4.8", Bio = "Pain management, injury recovery, mobility and rehabilitation plans.", AccentColor = "#F59E0B" },
            new() { Name = "Dr Ava Wilson", Specialty = "Mental Health Clinician", Department = "Mental Health", Availability = "Fri • 9:00 AM - 1:00 PM", Experience = "10 years", Rating = "4.9", Bio = "Stress, anxiety support, wellbeing plans and confidential counselling.", AccentColor = "#7C3AED" }
        };

        await _database.InsertAllAsync(doctors);
    }

    private async Task InitWithoutSeedAsync()
    {
        if (_database != null)
        {
            return;
        }

        SQLitePCL.Batteries_V2.Init();
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "medibook.db3");
        _database = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.Create | SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.SharedCache);
        await _database.CreateTableAsync<UserAccount>();
        await _database.CreateTableAsync<Doctor>();
        await _database.CreateTableAsync<Appointment>();
        await _database.CreateTableAsync<MedicalDocument>();
        await _database.CreateTableAsync<EmailReminder>();
    }

    public static string HashPassword(string password)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password.Trim()));
        return Convert.ToHexString(bytes);
    }

    public async Task<UserAccount> RegisterUserAsync(string fullName, string email, string phone, string dateOfBirth, string password)
    {
        await InitAsync();
        email = email.Trim().ToLowerInvariant();
        var existing = await _database!.Table<UserAccount>().Where(u => u.Email == email).FirstOrDefaultAsync();
        if (existing != null)
        {
            throw new InvalidOperationException("An account already exists with this email.");
        }

        var user = new UserAccount
        {
            FullName = fullName.Trim(),
            Email = email,
            Phone = phone.Trim(),
            DateOfBirth = dateOfBirth.Trim(),
            PasswordHash = HashPassword(password),
            AuthProvider = "Local",
            CreatedAt = DateTime.Now,
            LastLoginAt = DateTime.Now
        };

        await _database.InsertAsync(user);
        Preferences.Set(CurrentUserKey, user.Id);
        return user;
    }

    public async Task<UserAccount?> LoginAsync(string email, string password)
    {
        await InitAsync();
        email = email.Trim().ToLowerInvariant();
        var hash = HashPassword(password);
        var user = await _database!.Table<UserAccount>().Where(u => u.Email == email && u.PasswordHash == hash).FirstOrDefaultAsync();
        if (user != null)
        {
            user.LastLoginAt = DateTime.Now;
            await _database.UpdateAsync(user);
            Preferences.Set(CurrentUserKey, user.Id);
        }
        return user;
    }

    public async Task<UserAccount> SaveGoogleUserAsync(string fullName, string email, string googleSubject)
    {
        await InitAsync();
        email = email.Trim().ToLowerInvariant();
        var user = await _database!.Table<UserAccount>().Where(u => u.Email == email).FirstOrDefaultAsync();
        if (user == null)
        {
            user = new UserAccount
            {
                FullName = fullName,
                Email = email,
                Phone = string.Empty,
                DateOfBirth = string.Empty,
                PasswordHash = string.Empty,
                AuthProvider = "Google",
                GoogleSubject = googleSubject,
                CreatedAt = DateTime.Now,
                LastLoginAt = DateTime.Now
            };
            await _database.InsertAsync(user);
        }
        else
        {
            user.FullName = string.IsNullOrWhiteSpace(user.FullName) ? fullName : user.FullName;
            user.AuthProvider = "Google";
            user.GoogleSubject = googleSubject;
            user.LastLoginAt = DateTime.Now;
            await _database.UpdateAsync(user);
        }

        Preferences.Set(CurrentUserKey, user.Id);
        return user;
    }

    public async Task<UserAccount?> GetCurrentUserAsync()
    {
        await InitAsync();
        var id = Preferences.Get(CurrentUserKey, 0);
        if (id <= 0)
        {
            return null;
        }

        return await _database!.Table<UserAccount>().Where(u => u.Id == id).FirstOrDefaultAsync();
    }

    public void Logout()
    {
        Preferences.Remove(CurrentUserKey);
    }

    public async Task UpdateUserAsync(UserAccount user)
    {
        await InitAsync();
        await _database!.UpdateAsync(user);
    }

    public async Task<List<Doctor>> GetDoctorsAsync()
    {
        await InitAsync();
        return await _database!.Table<Doctor>().Where(d => d.IsActive).ToListAsync();
    }

    public async Task<Doctor?> GetDoctorAsync(int id)
    {
        await InitAsync();
        return await _database!.Table<Doctor>().Where(d => d.Id == id).FirstOrDefaultAsync();
    }

    public async Task<int> SaveAppointmentAsync(Appointment appointment)
    {
        await InitAsync();
        await _database!.InsertAsync(appointment);
        return appointment.Id;
    }

    public async Task<Appointment?> GetAppointmentAsync(int id)
    {
        await InitAsync();
        return await _database!.Table<Appointment>().Where(a => a.Id == id).FirstOrDefaultAsync();
    }

    public async Task<List<Appointment>> GetAppointmentsForCurrentUserAsync()
    {
        await InitAsync();
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return new List<Appointment>();
        }

        var appointments = await _database!.Table<Appointment>().Where(a => a.UserId == user.Id).ToListAsync();
        return appointments.OrderByDescending(a => a.DateText).ThenBy(a => a.TimeText).ToList();
    }

    public async Task<Appointment?> GetNextAppointmentAsync()
    {
        var appointments = await GetAppointmentsForCurrentUserAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        return appointments.Where(a => a.Status == "Upcoming" && string.Compare(a.DateText, today, StringComparison.Ordinal) >= 0)
                           .OrderBy(a => a.DateText)
                           .ThenBy(a => a.TimeText)
                           .FirstOrDefault();
    }

    public async Task SaveDocumentAsync(MedicalDocument document)
    {
        await InitAsync();
        await _database!.InsertAsync(document);
    }

    public async Task<List<MedicalDocument>> GetDocumentsForCurrentUserAsync()
    {
        await InitAsync();
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return new List<MedicalDocument>();
        }

        var docs = await _database!.Table<MedicalDocument>().Where(d => d.UserId == user.Id).ToListAsync();
        return docs.OrderByDescending(d => d.UploadedAt).ToList();
    }

    public async Task SaveEmailReminderAsync(EmailReminder reminder)
    {
        await InitAsync();
        await _database!.InsertAsync(reminder);
    }

    public async Task<List<EmailReminder>> GetDueEmailRemindersAsync()
    {
        await InitAsync();
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var user = await GetCurrentUserAsync();
        if (user == null)
        {
            return new List<EmailReminder>();
        }

        var reminders = await _database!.Table<EmailReminder>().Where(r => r.UserId == user.Id && r.Status == "Pending").ToListAsync();
        return reminders.Where(r => string.Compare(r.DueDateText, today, StringComparison.Ordinal) <= 0).ToList();
    }

    public async Task MarkEmailReminderSentAsync(EmailReminder reminder)
    {
        await InitAsync();
        reminder.Status = "Sent";
        reminder.SentAt = DateTime.Now;
        await _database!.UpdateAsync(reminder);
    }
}
