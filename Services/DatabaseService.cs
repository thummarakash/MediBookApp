using MediBook.Extensions;
using MediBook.Models;
using MediBook.Repositories;
using MediBook.Services.Auth;
using MediBook.Services.Firebase;
using MediBook.Services.Offline;

namespace MediBook.Services;

/// <summary>
/// Facade that bridges the existing ViewModel API to the real Firebase backend.
/// All ViewModels continue calling DatabaseService.Instance.*  — they don't need to change.
/// Firebase operations happen under the hood; the static mock data is kept as an offline fallback.
/// </summary>
public class DatabaseService
{
    private const string LoggedInKey = "medibook_logged_in";

    public static DatabaseService Instance { get; } = new();

    // ── Static seed data (offline fallback + first-run Firestore seeding) ───────

    public static readonly List<Clinic> StaticClinics = new()
    {
        new() { Id = 1, Name = "Melbourne Central Medical", Address = "123 Collins St, Melbourne VIC 3000", Latitude = -37.8142, Longitude = 144.9631 },
        new() { Id = 2, Name = "City Health Clinic", Address = "45 Bourke St, Melbourne VIC 3000", Latitude = -37.8136, Longitude = 144.9661 },
        new() { Id = 3, Name = "Family Care Centre", Address = "78 Swanston St, Carlton VIC 3053", Latitude = -37.8005, Longitude = 144.9634 },
        new() { Id = 4, Name = "Surat Central Hospital", Address = "Ring Road, Surat, Gujarat 395002", Latitude = 21.1895, Longitude = 72.8315 },
        new() { Id = 5, Name = "Adajan Medical Clinic", Address = "L.P. Savani Road, Adajan, Surat, Gujarat 395009", Latitude = 21.1964, Longitude = 72.7963 },
        new() { Id = 6, Name = "Vesu Family Care", Address = "Vip Road, Vesu, Surat, Gujarat 395007", Latitude = 21.1418, Longitude = 72.7845 },
        new() { Id = 7, Name = "Varachha Health Centre", Address = "Varachha Road, Surat, Gujarat 395006", Latitude = 21.2089, Longitude = 72.8624 },
        new() { Id = 8, Name = "Piplod Multispecialty Clinic", Address = "Dumas Road, Piplod, Surat, Gujarat 395007", Latitude = 21.1685, Longitude = 72.7794 }
    };

    public static readonly List<Doctor> StaticDoctors = new()
    {
        new() { Id = 1, Name = "Dr Emily Carter",  Specialty = "General Practitioner", Department = "General Care",  Availability = "Mon-Fri • 9:00 AM - 4:00 PM",  Experience = "9 years",  Rating = "4.9", Bio = "General health checks, family medicine and referrals.",                           FeePerAppointment = 85.00,  SlotDurationMinutes = 20, AccentColor = "#155EEF" },
        new() { Id = 2, Name = "Dr Michael Brown", Specialty = "Cardiologist",          Department = "Heart Clinic",  Availability = "Tue-Thu • 10:00 AM - 2:00 PM", Experience = "14 years", Rating = "4.8", Bio = "Cardiology consultations, blood pressure checks and ECG review.",          FeePerAppointment = 150.00, SlotDurationMinutes = 30, AccentColor = "#EF4444" },
        new() { Id = 3, Name = "Dr Sarah Wilson",  Specialty = "Dermatologist",         Department = "Skin Care",     Availability = "Wed-Fri • 11:30 AM - 5:00 PM", Experience = "7 years",  Rating = "4.7", Bio = "Mole mapping, skin cancer screening, and acne management.",             FeePerAppointment = 120.00, SlotDurationMinutes = 15, AccentColor = "#2DD4BF" }
    };

    public static readonly List<ClinicDoctor> StaticClinicDoctors = new()
    {
        new() { Id = 1, ClinicId = 1, DoctorId = 1 }, new() { Id = 2, ClinicId = 1, DoctorId = 2 },
        new() { Id = 3, ClinicId = 2, DoctorId = 1 }, new() { Id = 4, ClinicId = 2, DoctorId = 3 },
        new() { Id = 5, ClinicId = 3, DoctorId = 2 }, new() { Id = 6, ClinicId = 3, DoctorId = 3 },
        new() { Id = 7, ClinicId = 4, DoctorId = 1 }, new() { Id = 8, ClinicId = 5, DoctorId = 2 },
        new() { Id = 9, ClinicId = 6, DoctorId = 3 }, new() { Id = 10, ClinicId = 7, DoctorId = 1 },
        new() { Id = 11, ClinicId = 8, DoctorId = 2 }
    };

    private UserAccount? _currentUser;
    private DatabaseService() { }

    // ── Auth ─────────────────────────────────────────────────────────────────

    public async Task<UserAccount> RegisterUserAsync(string fullName, string email, string phone, string dateOfBirth, string password)
    {
        // Register in Firebase Auth
        var authResult = await FirebaseAuthService.Instance.SignUpWithEmailPasswordAsync(email, password, fullName);

        // Determine role (admin by email convention)
        var role = email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Patient";

        var user = new UserAccount
        {
            FirestoreId = authResult.UserId,
            Id = 1,
            FullName = fullName,
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Role = role,
            AuthProvider = "Local",
            CreatedAt = DateTime.Now
        };

        // Save profile to Firestore
        await UserRepository.Instance.CreateAsync(user);

        // Save session
        await SessionService.Instance.SaveSessionAsync(authResult, role);
        _currentUser = user;
        Preferences.Default.Set("medibook_onboarding_seen", true);

        return user;
    }

    public async Task<UserAccount?> LoginAsync(string email, string password)
    {
        var authResult = await FirebaseAuthService.Instance.SignInWithEmailPasswordAsync(email, password);

        // Load or create user profile from Firestore
        var user = await UserRepository.Instance.GetByIdAsync(authResult.UserId);
        if (user == null)
        {
            // First login after account existed before Firestore profile was created
            var role = email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Patient";
            user = new UserAccount
            {
                FirestoreId = authResult.UserId,
                Id = 1,
                FullName = authResult.DisplayName.IfEmpty(email.Split('@')[0]),
                Email = email,
                Role = role,
                AuthProvider = "Local"
            };
            await UserRepository.Instance.CreateAsync(user);
        }

        await SessionService.Instance.SaveSessionAsync(authResult, user.Role);
        _currentUser = user;
        Preferences.Default.Set("medibook_onboarding_seen", true);
        return user;
    }

    public async Task<UserAccount> SaveGoogleUserAsync(string fullName, string email, string googleSubject)
    {
        var userId = googleSubject;
        var user = await UserRepository.Instance.GetByIdAsync(userId);
        if (user == null)
        {
            user = new UserAccount
            {
                FirestoreId = userId,
                Id = 1,
                FullName = fullName,
                Email = email,
                Role = "Patient",
                AuthProvider = "Google"
            };
            await UserRepository.Instance.CreateAsync(user);
        }

        _currentUser = user;
        Preferences.Default.Set(LoggedInKey, true);
        Preferences.Default.Set("medibook_onboarding_seen", true);
        return user;
    }

    public async Task<UserAccount?> GetCurrentUserAsync()
    {
        if (_currentUser != null) return _currentUser;

        var userId = await SessionService.Instance.GetUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            if (!Preferences.Default.Get(LoggedInKey, false)) return null;
            // Legacy fallback for sessions stored before Firebase integration
            return CreateLegacyUser();
        }

        try
        {
            _currentUser = await UserRepository.Instance.GetByIdAsync(userId);
        }
        catch
        {
            _currentUser = CreateLegacyUser();
        }

        return _currentUser;
    }

    public bool IsLoggedIn => SessionService.Instance.IsAuthenticated
        || Preferences.Default.Get(LoggedInKey, false);

    public void Logout()
    {
        _currentUser = null;
        SessionService.Instance.SignOut();
    }

    public async Task UpdateUserAsync(UserAccount user)
    {
        _currentUser = user;
        if (!string.IsNullOrEmpty(user.FirestoreId))
            await UserRepository.Instance.UpdateAsync(user);
    }

    // ── Clinics ───────────────────────────────────────────────────────────────

    public async Task<List<Clinic>> GetClinicsAsync()
    {
        if (!Helpers.ConnectivityHelper.IsConnected)
        {
            var cached = await LocalCacheService.Instance.GetCachedClinicsAsync();
            return cached.Count > 0 ? cached : StaticClinics;
        }

        try
        {
            var clinics = await ClinicRepository.Instance.GetAllAsync();
            if (clinics.Count == 0)
            {
                await ClinicRepository.Instance.SeedIfEmptyAsync(StaticClinics);
                clinics = await ClinicRepository.Instance.GetAllAsync();
            }
            var result = clinics.Count > 0 ? clinics : StaticClinics;
            // Update offline cache in background
            _ = LocalCacheService.Instance.CacheClinicsAsync(result);
            return result;
        }
        catch
        {
            var cached = await LocalCacheService.Instance.GetCachedClinicsAsync();
            return cached.Count > 0 ? cached : StaticClinics;
        }
    }

    // ── Doctors ───────────────────────────────────────────────────────────────

    public async Task<List<Doctor>> GetDoctorsAsync()
    {
        if (!Helpers.ConnectivityHelper.IsConnected)
        {
            var cached = await LocalCacheService.Instance.GetCachedDoctorsAsync();
            return cached.Count > 0 ? cached : EnrichDoctorsWithClinic(StaticDoctors);
        }

        try
        {
            var doctors = await DoctorRepository.Instance.GetAllAsync();
            if (doctors.Count == 0)
            {
                await DoctorRepository.Instance.SeedIfEmptyAsync(StaticDoctors);
                doctors = await DoctorRepository.Instance.GetAllAsync();
            }
            var result = doctors.Count > 0 ? doctors : EnrichDoctorsWithClinic(StaticDoctors);
            _ = LocalCacheService.Instance.CacheDoctorsAsync(result);
            return result;
        }
        catch
        {
            var cached = await LocalCacheService.Instance.GetCachedDoctorsAsync();
            return cached.Count > 0 ? cached : EnrichDoctorsWithClinic(StaticDoctors);
        }
    }

    public async Task<Doctor?> GetDoctorAsync(int id)
    {
        var doctors = await GetDoctorsAsync();
        var doctor = doctors.FirstOrDefault(d => d.Id == id);
        if (doctor == null && id <= StaticDoctors.Count)
            doctor = StaticDoctors.FirstOrDefault(d => d.Id == id);
        return doctor;
    }

    public async Task<Doctor?> GetDoctorByFirestoreIdAsync(string firestoreId)
    {
        try { return await DoctorRepository.Instance.GetByIdAsync(firestoreId); }
        catch { return null; }
    }

    // ── Appointments ──────────────────────────────────────────────────────────

    public async Task<int> SaveAppointmentAsync(Appointment appointment)
    {
        try
        {
            var userId = await SessionService.Instance.GetUserIdAsync();
            appointment.UserFirestoreId = userId ?? string.Empty;
            var docId = await AppointmentRepository.Instance.CreateAsync(appointment);
            appointment.FirestoreId = docId;
            appointment.Id = Math.Abs(docId.GetHashCode() % 100000);
            return appointment.Id;
        }
        catch
        {
            appointment.Id = Math.Abs(Guid.NewGuid().GetHashCode() % 100000);
            return appointment.Id;
        }
    }

    public async Task<Appointment?> GetAppointmentAsync(int id)
    {
        var all = await GetAppointmentsForCurrentUserAsync();
        return all.FirstOrDefault(a => a.Id == id);
    }

    public async Task<Appointment?> GetAppointmentByFirestoreIdAsync(string firestoreId)
    {
        try { return await AppointmentRepository.Instance.GetByIdAsync(firestoreId); }
        catch { return null; }
    }

    public async Task<List<Appointment>> GetAppointmentsForCurrentUserAsync()
    {
        var userId = await SessionService.Instance.GetUserIdAsync();
        if (string.IsNullOrEmpty(userId)) return new List<Appointment>();

        if (!Helpers.ConnectivityHelper.IsConnected)
        {
            var cached = await LocalCacheService.Instance.GetCachedAppointmentsAsync(userId);
            for (int i = 0; i < cached.Count; i++) cached[i].Id = i + 1;
            return cached;
        }

        try
        {
            var appointments = await AppointmentRepository.Instance.GetByUserIdAsync(userId);
            for (int i = 0; i < appointments.Count; i++) appointments[i].Id = i + 1;
            _ = LocalCacheService.Instance.CacheAppointmentsAsync(userId, appointments);
            return appointments;
        }
        catch
        {
            var cached = await LocalCacheService.Instance.GetCachedAppointmentsAsync(userId);
            for (int i = 0; i < cached.Count; i++) cached[i].Id = i + 1;
            return cached;
        }
    }

    public async Task<Appointment?> GetNextAppointmentAsync()
    {
        try
        {
            var userId = await SessionService.Instance.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId)) return null;
            return await AppointmentRepository.Instance.GetNextUpcomingAsync(userId);
        }
        catch { return null; }
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        try { return await AppointmentRepository.Instance.GetAllAsync(); }
        catch { return new List<Appointment>(); }
    }

    // ── Documents ─────────────────────────────────────────────────────────────

    public async Task SaveDocumentAsync(MedicalDocument document)
    {
        try
        {
            var userId = await SessionService.Instance.GetUserIdAsync();
            document.UserFirestoreId = userId ?? string.Empty;
            await DocumentRepository.Instance.CreateAsync(document);
            document.Id = Math.Abs(document.FirestoreId.GetHashCode() % 100000);
        }
        catch
        {
            document.Id = Math.Abs(Guid.NewGuid().GetHashCode() % 100000);
        }
    }

    public async Task<List<MedicalDocument>> GetDocumentsForCurrentUserAsync()
    {
        try
        {
            var userId = await SessionService.Instance.GetUserIdAsync();
            if (string.IsNullOrEmpty(userId)) return new List<MedicalDocument>();
            return await DocumentRepository.Instance.GetByUserIdAsync(userId);
        }
        catch { return new List<MedicalDocument>(); }
    }

    public async Task DeleteDocumentAsync(int documentId)
    {
        // Find by local ID
        var all = await GetDocumentsForCurrentUserAsync();
        var doc = all.FirstOrDefault(d => d.Id == documentId);
        if (doc != null && !string.IsNullOrEmpty(doc.FirestoreId))
        {
            if (!string.IsNullOrEmpty(doc.StoragePath))
            {
                try { await Firebase.FirebaseStorageService.Instance.DeleteFileAsync(doc.StoragePath); }
                catch { /* Storage delete failure is non-critical */ }
            }
            await DocumentRepository.Instance.DeleteAsync(doc.FirestoreId);
        }
    }

    // ── Email reminders ───────────────────────────────────────────────────────

    public async Task SaveEmailReminderAsync(EmailReminder reminder)
        => await Task.CompletedTask; // Handled by EmailNotificationService

    public async Task<List<EmailReminder>> GetDueEmailRemindersAsync()
        => new List<EmailReminder>();

    public async Task MarkEmailReminderSentAsync(EmailReminder reminder)
        => await Task.CompletedTask;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static List<Doctor> EnrichDoctorsWithClinic(List<Doctor> doctors)
    {
        foreach (var doctor in doctors)
        {
            var mapping = StaticClinicDoctors.FirstOrDefault(cd => cd.DoctorId == doctor.Id);
            if (mapping != null)
            {
                var clinic = StaticClinics.FirstOrDefault(c => c.Id == mapping.ClinicId);
                if (clinic != null) doctor.ClinicName = clinic.Name;
            }
        }
        return doctors;
    }

    private static UserAccount CreateLegacyUser() => new()
    {
        Id = 1, FirestoreId = string.Empty,
        FullName = "MediBook User",
        Email = "user@medibook.com",
        Role = "Patient"
    };
}
