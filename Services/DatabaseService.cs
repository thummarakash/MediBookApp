using MediBook.Configuration;
using MediBook.Extensions;
using MediBook.Models;
using MediBook.Repositories;
using MediBook.Services.Auth;
using MediBook.Services.Firebase;
using MediBook.Services.Offline;

namespace MediBook.Services;

public class DatabaseService
{
    public static DatabaseService Instance { get; } = new();

    public static readonly List<Clinic> DefaultClinics = new()
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

    public static readonly List<Doctor> DefaultDoctors = new()
    {
        new() { Id = 1, Name = "Dr Emily Carter",  Specialty = "General Practitioner", Department = "General Care",  Availability = "Mon-Fri • 9:00 AM - 4:00 PM",  Experience = "9 years",  Rating = "4.9", Bio = "General health checks, family medicine and referrals.",                           FeePerAppointment = 85.00,  SlotDurationMinutes = 20, AccentColor = "#155EEF" },
        new() { Id = 2, Name = "Dr Michael Brown", Specialty = "Cardiologist",          Department = "Heart Clinic",  Availability = "Tue-Thu • 10:00 AM - 2:00 PM", Experience = "14 years", Rating = "4.8", Bio = "Cardiology consultations, blood pressure checks and ECG review.",          FeePerAppointment = 150.00, SlotDurationMinutes = 30, AccentColor = "#EF4444" },
        new() { Id = 3, Name = "Dr Sarah Wilson",  Specialty = "Dermatologist",         Department = "Skin Care",     Availability = "Wed-Fri • 11:30 AM - 5:00 PM", Experience = "7 years",  Rating = "4.7", Bio = "Mole mapping, skin cancer screening, and acne management.",             FeePerAppointment = 120.00, SlotDurationMinutes = 15, AccentColor = "#2DD4BF" }
    };

    public static readonly List<ClinicDoctor> DefaultClinicDoctors = new()
    {
        new() { Id = 1, ClinicId = 1, DoctorId = 1 }, new() { Id = 2, ClinicId = 1, DoctorId = 2 },
        new() { Id = 3, ClinicId = 2, DoctorId = 1 }, new() { Id = 4, ClinicId = 2, DoctorId = 3 },
        new() { Id = 5, ClinicId = 3, DoctorId = 2 }, new() { Id = 6, ClinicId = 3, DoctorId = 3 },
        new() { Id = 7, ClinicId = 4, DoctorId = 1 }, new() { Id = 8, ClinicId = 5, DoctorId = 2 },
        new() { Id = 9, ClinicId = 6, DoctorId = 3 }, new() { Id = 10, ClinicId = 7, DoctorId = 1 },
        new() { Id = 11, ClinicId = 8, DoctorId = 2 }
    };

    public static List<Clinic> StaticClinics => DefaultClinics;
    public static List<Doctor> StaticDoctors => DefaultDoctors;
    public static List<ClinicDoctor> StaticClinicDoctors => DefaultClinicDoctors;

    private UserAccount? _currentUser;
    private DatabaseService() { }

    public async Task<UserAccount> RegisterUserAsync(string fullName, string email, string phone, string dateOfBirth, string password)
    {
        var authResult = await FirebaseAuthService.Instance.SignUpWithEmailPasswordAsync(email, password, fullName);
        var userRole = email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Patient";
        var fcmToken = Preferences.Default.Get(AppConfig.PrefKeys.FcmToken, string.Empty);

        var user = new UserAccount
        {
            FirestoreId = authResult.UserId,
            Id = 1,
            FullName = fullName,
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Role = userRole,
            AuthProvider = "Local",
            FCMToken = fcmToken,
            CreatedAt = DateTime.Now
        };

        await SessionService.Instance.SaveSessionAsync(authResult, userRole);
        await UserRepository.Instance.CreateAsync(user);

        _currentUser = user;
        Preferences.Default.Set(AppConfig.PrefKeys.OnboardingSeen, true);
        return user;
    }

    public async Task<UserAccount?> LoginAsync(string email, string password)
    {
        var authResult = await FirebaseAuthService.Instance.SignInWithEmailPasswordAsync(email, password);
        var user = await UserRepository.Instance.GetByIdAsync(authResult.UserId);
        var fcmToken = Preferences.Default.Get(AppConfig.PrefKeys.FcmToken, string.Empty);

        if (user == null)
        {
            var userRole = email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Patient";
            user = new UserAccount
            {
                FirestoreId = authResult.UserId,
                Id = 1,
                FullName = authResult.DisplayName.IfEmpty(email.Split('@')[0]),
                Email = email,
                Role = userRole,
                FCMToken = fcmToken,
                AuthProvider = "Local"
            };
            await UserRepository.Instance.CreateAsync(user);
        }
        else
        {
            user.FCMToken = fcmToken;
            await UserRepository.Instance.UpdateAsync(user);
        }

        await SessionService.Instance.SaveSessionAsync(authResult, user.Role);
        _currentUser = user;
        Preferences.Default.Set(AppConfig.PrefKeys.OnboardingSeen, true);
        return user;
    }

    public async Task<UserAccount> SaveGoogleUserAsync(string fullName, string email, string googleSubject, string photoUrl = "")
    {
        var userId = googleSubject;
        var user = await UserRepository.Instance.GetByIdAsync(userId);
        var fcmToken = Preferences.Default.Get(AppConfig.PrefKeys.FcmToken, string.Empty);

        if (user == null)
        {
            user = new UserAccount
            {
                FirestoreId = userId,
                Id = 1,
                FullName = fullName,
                Email = email,
                AvatarUrl = photoUrl,
                Role = "Patient",
                FCMToken = fcmToken,
                AuthProvider = "Google"
            };
            await UserRepository.Instance.CreateAsync(user);
        }
        else
        {
            bool needsUpdate = false;
            if (!string.IsNullOrEmpty(photoUrl) && string.IsNullOrEmpty(user.AvatarUrl))
            {
                user.AvatarUrl = photoUrl;
                needsUpdate = true;
            }
            if (user.FCMToken != fcmToken)
            {
                user.FCMToken = fcmToken;
                needsUpdate = true;
            }
            if (needsUpdate)
                await UserRepository.Instance.UpdateAsync(user);
        }

        _currentUser = user;
        Preferences.Default.Set(AppConfig.PrefKeys.LoggedIn, true);
        Preferences.Default.Set(AppConfig.PrefKeys.OnboardingSeen, true);
        return user;
    }

    public async Task<UserAccount?> GetCurrentUserAsync()
    {
        if (_currentUser != null) return _currentUser;

        var userId = await SessionService.Instance.GetUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        try
        {
            _currentUser = await UserRepository.Instance.GetByIdAsync(userId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetCurrentUserAsync failed: {ex.Message}");
            return null;
        }

        return _currentUser;
    }

    public bool IsLoggedIn => SessionService.Instance.IsAuthenticated
        || Preferences.Default.Get(AppConfig.PrefKeys.LoggedIn, false);

    public async Task LogoutAsync()
    {
        try
        {
            var userId = await SessionService.Instance.GetUserIdAsync();
            if (!string.IsNullOrEmpty(userId))
            {
                try
                {
                    await UserRepository.Instance.UpdateFcmTokenAsync(userId, "");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DatabaseService] Logout: FCM token clear failed: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] Logout: Failed to retrieve user ID: {ex.Message}");
        }

        _currentUser = null;
        SessionService.Instance.SignOut();

        Preferences.Default.Set(AppConfig.PrefKeys.LoggedIn, false);
        Preferences.Default.Set(AppConfig.PrefKeys.BiometricEnabled, false);
        Preferences.Default.Set(AppConfig.PrefKeys.PinEnabled, false);
        Preferences.Default.Remove(AppConfig.PrefKeys.PinValue);
    }

    public async Task UpdateUserAsync(UserAccount user)
    {
        _currentUser = user;
        if (!string.IsNullOrEmpty(user.FirestoreId))
            await UserRepository.Instance.UpdateAsync(user);
    }

    public async Task<List<Clinic>> GetClinicsAsync()
    {
        if (!Helpers.ConnectivityHelper.IsConnected)
        {
            return await LocalCacheService.Instance.GetCachedClinicsAsync();
        }

        try
        {
            var clinics = await ClinicRepository.Instance.GetAllAsync();
            _ = LocalCacheService.Instance.CacheClinicsAsync(clinics);
            return clinics;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetClinicsAsync failed, loading cache: {ex.Message}");
            return await LocalCacheService.Instance.GetCachedClinicsAsync();
        }
    }

    public async Task<List<Doctor>> GetDoctorsAsync()
    {
        if (!Helpers.ConnectivityHelper.IsConnected)
        {
            return await LocalCacheService.Instance.GetCachedDoctorsAsync();
        }

        try
        {
            var doctors = await DoctorRepository.Instance.GetAllAsync();
            _ = LocalCacheService.Instance.CacheDoctorsAsync(doctors);
            return doctors;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetDoctorsAsync failed, loading cache: {ex.Message}");
            return await LocalCacheService.Instance.GetCachedDoctorsAsync();
        }
    }

    public async Task<Doctor?> GetDoctorAsync(int id)
    {
        var allDoctors = await GetDoctorsAsync();
        var doctor = allDoctors.FirstOrDefault(d => d.Id == id);
        if (doctor == null && id <= DefaultDoctors.Count)
            doctor = DefaultDoctors.FirstOrDefault(d => d.Id == id);
        return doctor;
    }

    public async Task<Doctor?> GetDoctorByFirestoreIdAsync(string firestoreId)
    {
        try
        {
            return await DoctorRepository.Instance.GetByIdAsync(firestoreId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetDoctorByFirestoreIdAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<int> SaveAppointmentAsync(Appointment appointment)
    {
        try
        {
            var userId = await SessionService.Instance.GetUserIdAsync();
            appointment.UserFirestoreId = userId ?? string.Empty;
            var newDocId = await AppointmentRepository.Instance.CreateAsync(appointment);
            appointment.FirestoreId = newDocId;
            appointment.Id = Math.Abs(newDocId.GetHashCode() % 100000);
            return appointment.Id;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] SaveAppointmentAsync failed, generating offline ID: {ex.Message}");
            appointment.Id = Math.Abs(Guid.NewGuid().GetHashCode() % 100000);
            return appointment.Id;
        }
    }

    public async Task<Appointment?> GetAppointmentAsync(int id)
    {
        var appointments = await GetAppointmentsForCurrentUserAsync();
        return appointments.FirstOrDefault(a => a.Id == id);
    }

    public async Task<Appointment?> GetAppointmentByFirestoreIdAsync(string firestoreId)
    {
        try
        {
            return await AppointmentRepository.Instance.GetByIdAsync(firestoreId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetAppointmentByFirestoreIdAsync failed: {ex.Message}");
            return null;
        }
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetAppointmentsForCurrentUserAsync failed, loading cache: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetNextAppointmentAsync failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        try
        {
            return await AppointmentRepository.Instance.GetAllAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetAllAppointmentsAsync failed: {ex.Message}");
            return new List<Appointment>();
        }
    }

    public async Task SaveDocumentAsync(MedicalDocument document)
    {
        try
        {
            var userId = await SessionService.Instance.GetUserIdAsync();
            document.UserFirestoreId = userId ?? string.Empty;
            await DocumentRepository.Instance.CreateAsync(document);
            document.Id = Math.Abs(document.FirestoreId.GetHashCode() % 100000);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] SaveDocumentAsync failed: {ex.Message}");
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetDocumentsForCurrentUserAsync failed: {ex.Message}");
            return new List<MedicalDocument>();
        }
    }

    public async Task DeleteDocumentAsync(int documentId)
    {
        var documents = await GetDocumentsForCurrentUserAsync();
        var document = documents.FirstOrDefault(d => d.Id == documentId);
        if (document != null && !string.IsNullOrEmpty(document.FirestoreId))
        {
            if (!string.IsNullOrEmpty(document.StoragePath))
            {
                try
                {
                    await Firebase.FirebaseStorageService.Instance.DeleteFileAsync(document.StoragePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[DatabaseService] DeleteDocumentAsync: Storage delete failed (non-critical): {ex.Message}");
                }
            }
            await DocumentRepository.Instance.DeleteAsync(document.FirestoreId);
        }
    }

    public async Task SaveEmailReminderAsync(EmailReminder reminder)
        => await Task.CompletedTask;

    public async Task<List<EmailReminder>> GetDueEmailRemindersAsync()
        => new List<EmailReminder>();

    public async Task MarkEmailReminderSentAsync(EmailReminder reminder)
        => await Task.CompletedTask;

    public async Task<List<UserAccount>> GetAllUsersAsync()
    {
        try
        {
            return await UserRepository.Instance.GetAllAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DatabaseService] GetAllUsersAsync failed: {ex.Message}");
            return new List<UserAccount>();
        }
    }

    public async Task SeedDefaultAdminAsync()
    {
        await AdminSeeder.SeedAdminAsync();
    }

    private static List<Doctor> EnrichDoctorsWithClinic(List<Doctor> doctors)
    {
        foreach (var doctor in doctors)
        {
            var clinicDoctorLink = DefaultClinicDoctors.FirstOrDefault(cd => cd.DoctorId == doctor.Id);
            if (clinicDoctorLink != null)
            {
                var clinic = DefaultClinics.FirstOrDefault(c => c.Id == clinicDoctorLink.ClinicId);
                if (clinic != null) doctor.ClinicName = clinic.Name;
            }
        }
        return doctors;
    }
}
