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
    private const string logged_in_key = "medibook_logged_in";

    public static DatabaseService Instance { get; } = new();

    // Default static clinics fallback for offline usage
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

    private UserAccount? _currUser;
    private DatabaseService() { }

    public async Task<UserAccount> RegisterUserAsync(string fullName, string email, string phone, string dateOfBirth, string password)
    {
        // SignUp new patient via FirebaseAuth
        var auth_res = await FirebaseAuthService.Instance.SignUpWithEmailPasswordAsync(email, password, fullName);
        var usr_role = email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Patient";
        var fcm_tok = Preferences.Default.Get(AppConfig.PrefKeys.FcmToken, string.Empty);

        var usr = new UserAccount
        {
            FirestoreId = auth_res.UserId,
            Id = 1,
            FullName = fullName,
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Role = usr_role,
            AuthProvider = "Local",
            FCMToken = fcm_tok,
            CreatedAt = DateTime.Now
        };

        // Save session data first to prevent auth issue in firestore rule
        await SessionService.Instance.SaveSessionAsync(auth_res, usr_role);
        await UserRepository.Instance.CreateAsync(usr);

        _currUser = usr;
        Preferences.Default.Set("medibook_onboarding_seen", true);
        return usr;
    }

    public async Task<UserAccount?> LoginAsync(string email, string password)
    {
        var auth_res = await FirebaseAuthService.Instance.SignInWithEmailPasswordAsync(email, password);
        var usr = await UserRepository.Instance.GetByIdAsync(auth_res.UserId);
        var fcm_tok = Preferences.Default.Get(AppConfig.PrefKeys.FcmToken, string.Empty);
        
        if (usr == null)
        {
            var usr_role = email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Patient";
            usr = new UserAccount
            {
                FirestoreId = auth_res.UserId,
                Id = 1,
                FullName = auth_res.DisplayName.IfEmpty(email.Split('@')[0]),
                Email = email,
                Role = usr_role,
                FCMToken = fcm_tok,
                AuthProvider = "Local"
            };
            await UserRepository.Instance.CreateAsync(usr);
        }
        else
        {
            usr.FCMToken = fcm_tok;
            await UserRepository.Instance.UpdateAsync(usr);
        }

        await SessionService.Instance.SaveSessionAsync(auth_res, usr.Role);
        _currUser = usr;
        Preferences.Default.Set("medibook_onboarding_seen", true);
        return usr;
    }

    public async Task<UserAccount> SaveGoogleUserAsync(string fullName, string email, string googleSubject, string photoUrl = "")
    {
        var u_id = googleSubject;
        var usr = await UserRepository.Instance.GetByIdAsync(u_id);
        var fcm_tok = Preferences.Default.Get(AppConfig.PrefKeys.FcmToken, string.Empty);
        
        if (usr == null)
        {
            usr = new UserAccount
            {
                FirestoreId = u_id,
                Id = 1,
                FullName = fullName,
                Email = email,
                AvatarUrl = photoUrl,
                Role = "Patient",
                FCMToken = fcm_tok,
                AuthProvider = "Google"
            };
            await UserRepository.Instance.CreateAsync(usr);
        }
        else
        {
            bool need_upd = false;
            if (!string.IsNullOrEmpty(photoUrl) && string.IsNullOrEmpty(usr.AvatarUrl))
            {
                usr.AvatarUrl = photoUrl;
                need_upd = true;
            }
            if (usr.FCMToken != fcm_tok)
            {
                usr.FCMToken = fcm_tok;
                need_upd = true;
            }
            if (need_upd)
            {
                await UserRepository.Instance.UpdateAsync(usr);
            }
        }

        _currUser = usr;
        Preferences.Default.Set(logged_in_key, true);
        Preferences.Default.Set("medibook_onboarding_seen", true);
        return usr;
    }

    public async Task<UserAccount?> GetCurrentUserAsync()
    {
        if (_currUser != null) return _currUser;

        var uid = await SessionService.Instance.GetUserIdAsync();
        if (string.IsNullOrEmpty(uid))
        {
            if (!Preferences.Default.Get(logged_in_key, false)) return null;
            return CreateLegacyUser();
        }

        try
        {
            _currUser = await UserRepository.Instance.GetByIdAsync(uid);
        }
        catch (Exception db_err)
        {
            System.Diagnostics.Debug.WriteLine($"[dbSvc] GetCurrentUserAsync query error: {db_err.Message}");
            _currUser = CreateLegacyUser();
        }

        return _currUser;
    }

    public bool IsLoggedIn => SessionService.Instance.IsAuthenticated
        || Preferences.Default.Get(logged_in_key, false);

    public void Logout()
    {
        try
        {
            var uid = SessionService.Instance.GetUserIdAsync().GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(uid))
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await UserRepository.Instance.UpdateFcmTokenAsync(uid, "");
                    }
                    catch (Exception err)
                    {
                        System.Diagnostics.Debug.WriteLine($"[dbSvc] Couldn't clear FCM push token: {err.Message}");
                    }
                });
            }
        }
        catch (Exception err)
        {
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Logout failed to fetch uid: {err.Message}");
        }

        _currUser = null;
        SessionService.Instance.SignOut();

        Preferences.Default.Set(logged_in_key, false);
        Preferences.Default.Set(AppConfig.PrefKeys.LoggedIn, false);
        Preferences.Default.Set(AppConfig.PrefKeys.BiometricEnabled, false);
        Preferences.Default.Set(AppConfig.PrefKeys.PinEnabled, false);
        Preferences.Default.Remove(AppConfig.PrefKeys.PinValue);
    }

    public async Task UpdateUserAsync(UserAccount user)
    {
        _currUser = user;
        if (!string.IsNullOrEmpty(user.FirestoreId))
            await UserRepository.Instance.UpdateAsync(user);
    }

    public async Task<List<Clinic>> GetClinicsAsync()
    {
        // Return local clinics if offline
        if (!Helpers.ConnectivityHelper.IsConnected)
        {
            var cached_list = await LocalCacheService.Instance.GetCachedClinicsAsync();
            return cached_list.Count > 0 ? cached_list : DefaultClinics;
        }

        try
        {
            var items = await ClinicRepository.Instance.GetAllAsync();
            if (items.Count == 0)
            {
                await ClinicRepository.Instance.SeedIfEmptyAsync(DefaultClinics);
                items = await ClinicRepository.Instance.GetAllAsync();
            }
            var res = items.Count > 0 ? items : DefaultClinics;
            _ = LocalCacheService.Instance.CacheClinicsAsync(res);
            return res;
        }
        catch (Exception conn_err)
        {
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Fetching clinics from cache because of error: {conn_err.Message}");
            var cached_list = await LocalCacheService.Instance.GetCachedClinicsAsync();
            return cached_list.Count > 0 ? cached_list : DefaultClinics;
        }
    }

    public async Task<List<Doctor>> GetDoctorsAsync()
    {
        if (!Helpers.ConnectivityHelper.IsConnected)
        {
            var cached_list = await LocalCacheService.Instance.GetCachedDoctorsAsync();
            return cached_list.Count > 0 ? cached_list : EnrichDoctorsWithClinic(DefaultDoctors);
        }

        try
        {
            var items = await DoctorRepository.Instance.GetAllAsync();
            if (items.Count == 0)
            {
                await DoctorRepository.Instance.SeedIfEmptyAsync(DefaultDoctors);
                items = await DoctorRepository.Instance.GetAllAsync();
            }
            var res = items.Count > 0 ? items : EnrichDoctorsWithClinic(DefaultDoctors);
            _ = LocalCacheService.Instance.CacheDoctorsAsync(res);
            return res;
        }
        catch (Exception conn_err)
        {
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Fetching doctors from cache because of error: {conn_err.Message}");
            var cached_list = await LocalCacheService.Instance.GetCachedDoctorsAsync();
            return cached_list.Count > 0 ? cached_list : EnrichDoctorsWithClinic(DefaultDoctors);
        }
    }

    public async Task<Doctor?> GetDoctorAsync(int id)
    {
        var all_docs = await GetDoctorsAsync();
        var doc = all_docs.FirstOrDefault(d => d.Id == id);
        if (doc == null && id <= DefaultDoctors.Count)
            doc = DefaultDoctors.FirstOrDefault(d => d.Id == id);
        return doc;
    }

    public async Task<Doctor?> GetDoctorByFirestoreIdAsync(string firestoreId)
    {
        try 
        { 
            return await DoctorRepository.Instance.GetByIdAsync(firestoreId); 
        }
        catch (Exception doc_err)
        { 
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Doctor not found in Firestore: {doc_err.Message}");
            return null; 
        }
    }

    public async Task<int> SaveAppointmentAsync(Appointment appointment)
    {
        try
        {
            var uid = await SessionService.Instance.GetUserIdAsync();
            appointment.UserFirestoreId = uid ?? string.Empty;
            var db_id = await AppointmentRepository.Instance.CreateAsync(appointment);
            appointment.FirestoreId = db_id;
            appointment.Id = Math.Abs(db_id.GetHashCode() % 100000);
            return appointment.Id;
        }
        catch (Exception save_err)
        {
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Error saving appointment, generating offline UUID: {save_err.Message}");
            appointment.Id = Math.Abs(Guid.NewGuid().GetHashCode() % 100000);
            return appointment.Id;
        }
    }

    public async Task<Appointment?> GetAppointmentAsync(int id)
    {
        var all_appts = await GetAppointmentsForCurrentUserAsync();
        return all_appts.FirstOrDefault(a => a.Id == id);
    }

    public async Task<Appointment?> GetAppointmentByFirestoreIdAsync(string firestoreId)
    {
        try 
        { 
            return await AppointmentRepository.Instance.GetByIdAsync(firestoreId); 
        }
        catch (Exception appt_err)
        { 
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Appointment not found in Firestore: {appt_err.Message}");
            return null; 
        }
    }

    public async Task<List<Appointment>> GetAppointmentsForCurrentUserAsync()
    {
        var uid = await SessionService.Instance.GetUserIdAsync();
        if (string.IsNullOrEmpty(uid)) return new List<Appointment>();

        if (!Helpers.ConnectivityHelper.IsConnected)
        {
            var cached_items = await LocalCacheService.Instance.GetCachedAppointmentsAsync(uid);
            for (int i = 0; i < cached_items.Count; i++) cached_items[i].Id = i + 1;
            return cached_items;
        }

        try
        {
            var db_items = await AppointmentRepository.Instance.GetByUserIdAsync(uid);
            for (int i = 0; i < db_items.Count; i++) db_items[i].Id = i + 1;
            _ = LocalCacheService.Instance.CacheAppointmentsAsync(uid, db_items);
            return db_items;
        }
        catch (Exception fetch_err)
        {
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Error fetching user appointments, loading cache: {fetch_err.Message}");
            var cached_items = await LocalCacheService.Instance.GetCachedAppointmentsAsync(uid);
            for (int i = 0; i < cached_items.Count; i++) cached_items[i].Id = i + 1;
            return cached_items;
        }
    }

    public async Task<Appointment?> GetNextAppointmentAsync()
    {
        try
        {
            var uid = await SessionService.Instance.GetUserIdAsync();
            if (string.IsNullOrEmpty(uid)) return null;
            return await AppointmentRepository.Instance.GetNextUpcomingAsync(uid);
        }
        catch (Exception next_err)
        { 
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Next appointment load error: {next_err.Message}");
            return null; 
        }
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        try 
        { 
            return await AppointmentRepository.Instance.GetAllAsync(); 
        }
        catch (Exception all_err)
        { 
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Fetch all appointments failed: {all_err.Message}");
            return new List<Appointment>(); 
        }
    }

    public async Task SaveDocumentAsync(MedicalDocument document)
    {
        try
        {
            var uid = await SessionService.Instance.GetUserIdAsync();
            document.UserFirestoreId = uid ?? string.Empty;
            await DocumentRepository.Instance.CreateAsync(document);
            document.Id = Math.Abs(document.FirestoreId.GetHashCode() % 100000);
        }
        catch (Exception doc_save_err)
        {
            System.Diagnostics.Debug.WriteLine($"[dbSvc] SaveDocumentAsync query failed: {doc_save_err.Message}");
            document.Id = Math.Abs(Guid.NewGuid().GetHashCode() % 100000);
        }
    }

    public async Task<List<MedicalDocument>> GetDocumentsForCurrentUserAsync()
    {
        try
        {
            var uid = await SessionService.Instance.GetUserIdAsync();
            if (string.IsNullOrEmpty(uid)) return new List<MedicalDocument>();
            return await DocumentRepository.Instance.GetByUserIdAsync(uid);
        }
        catch (Exception doc_fetch_err)
        { 
            System.Diagnostics.Debug.WriteLine($"[dbSvc] Load documents query failed: {doc_fetch_err.Message}");
            return new List<MedicalDocument>(); 
        }
    }

    public async Task DeleteDocumentAsync(int documentId)
    {
        var all_docs = await GetDocumentsForCurrentUserAsync();
        var target_doc = all_docs.FirstOrDefault(d => d.Id == documentId);
        if (target_doc != null && !string.IsNullOrEmpty(target_doc.FirestoreId))
        {
            if (!string.IsNullOrEmpty(target_doc.StoragePath))
            {
                try 
                { 
                    await Firebase.FirebaseStorageService.Instance.DeleteFileAsync(target_doc.StoragePath); 
                }
                catch (Exception del_err)
                { 
                    System.Diagnostics.Debug.WriteLine($"[dbSvc] DeleteDocumentAsync non-critical storage delete failure: {del_err.Message}");
                }
            }
            await DocumentRepository.Instance.DeleteAsync(target_doc.FirestoreId);
        }
    }

    public async Task SaveEmailReminderAsync(EmailReminder reminder)
        => await Task.CompletedTask;

    public async Task<List<EmailReminder>> GetDueEmailRemindersAsync()
        => new List<EmailReminder>();

    public async Task MarkEmailReminderSentAsync(EmailReminder reminder)
        => await Task.CompletedTask;

    public async Task SeedDefaultAdminAsync()
    {
        try
        {
            var auth_res = await FirebaseAuthService.Instance.SignInWithEmailPasswordAsync(
                "akashthummarau@gmail.com",
                "Admin@!23"
            );
            
            var admin_profile = await UserRepository.Instance.GetByIdAsync(auth_res.UserId);
            if (admin_profile == null)
            {
                var new_admin = new UserAccount
                {
                    FirestoreId = auth_res.UserId,
                    Id = 1,
                    FullName = "System Admin",
                    Email = "akashthummarau@gmail.com",
                    Phone = "+91 9999999999",
                    DateOfBirth = "01/01/1990",
                    Role = "Admin",
                    AuthProvider = "Local",
                    CreatedAt = DateTime.Now
                };
                await UserRepository.Instance.CreateAsync(new_admin);
            }
        }
        catch (Exception auth_err)
        {
            var msg = auth_err.Message;
            if (msg.Contains("EMAIL_NOT_FOUND") || msg.Contains("INVALID_LOGIN_CREDENTIALS") || msg.Contains("INVALID_EMAIL"))
            {
                try
                {
                    var auth_res = await FirebaseAuthService.Instance.SignUpWithEmailPasswordAsync(
                        "akashthummarau@gmail.com",
                        "Admin@!23",
                        "System Admin"
                    );

                    var new_admin = new UserAccount
                    {
                        FirestoreId = auth_res.UserId,
                        Id = 1,
                        FullName = "System Admin",
                        Email = "akashthummarau@gmail.com",
                        Phone = "+91 9999999999",
                        DateOfBirth = "01/01/1990",
                        Role = "Admin",
                        AuthProvider = "Local",
                        CreatedAt = DateTime.Now
                    };
                    await UserRepository.Instance.CreateAsync(new_admin);
                }
                catch (Exception signup_err)
                {
                    System.Diagnostics.Debug.WriteLine($"[dbSvc] Admin auto-signup failed: {signup_err.Message}");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[dbSvc] Admin auto-login check failed: {auth_err.Message}");
            }
        }
    }

    private static List<Doctor> EnrichDoctorsWithClinic(List<Doctor> doctors)
    {
        foreach (var d in doctors)
        {
            var m = DefaultClinicDoctors.FirstOrDefault(cd => cd.DoctorId == d.Id);
            if (m != null)
            {
                var c = DefaultClinics.FirstOrDefault(clin => clin.Id == m.ClinicId);
                if (c != null) d.ClinicName = c.Name;
            }
        }
        return doctors;
    }

    private static UserAccount CreateLegacyUser() => new()
    {
        Id = 1, 
        FirestoreId = string.Empty,
        FullName = "MediBook User",
        Email = "user@medibook.com",
        Role = "Patient"
    };
}
