using MediBook.Models;

namespace MediBook.Services;

public class DatabaseService
{
    private const string CurrentUserKey = "medibook_current_user_id";
    private const string LoggedInKey = "medibook_logged_in";
    public static DatabaseService Instance { get; } = new();

    // Mock Clinics — Melbourne & Surat (Testing)
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

    // Mock Doctors — as required
    public static readonly List<Doctor> StaticDoctors = new()
    {
        new() { Id = 1, Name = "Dr Emily Carter", Specialty = "General Practitioner", Department = "General Care", Availability = "Mon-Fri • 9:00 AM - 4:00 PM", Experience = "9 years", Rating = "4.9", Bio = "General health checks, family medicine and referrals.", FeePerAppointment = 85.00, SlotDurationMinutes = 20, AccentColor = "#155EEF" },
        new() { Id = 2, Name = "Dr Michael Brown", Specialty = "Cardiologist", Department = "Heart Clinic", Availability = "Tue-Thu • 10:00 AM - 2:00 PM", Experience = "14 years", Rating = "4.8", Bio = "Cardiology consultations, blood pressure checks and ECG review.", FeePerAppointment = 150.00, SlotDurationMinutes = 30, AccentColor = "#EF4444" },
        new() { Id = 3, Name = "Dr Sarah Wilson", Specialty = "Dermatologist", Department = "Skin Care", Availability = "Wed-Fri • 11:30 AM - 5:00 PM", Experience = "7 years", Rating = "4.7", Bio = "Mole mapping, skin cancer screening, and acne management.", FeePerAppointment = 120.00, SlotDurationMinutes = 15, AccentColor = "#2DD4BF" }
    };

    // Many-to-Many — which doctors work at which clinics
    public static readonly List<ClinicDoctor> StaticClinicDoctors = new()
    {
        new() { Id = 1, ClinicId = 1, DoctorId = 1 },
        new() { Id = 2, ClinicId = 1, DoctorId = 2 },
        new() { Id = 3, ClinicId = 2, DoctorId = 1 },
        new() { Id = 4, ClinicId = 2, DoctorId = 3 },
        new() { Id = 5, ClinicId = 3, DoctorId = 2 },
        new() { Id = 6, ClinicId = 3, DoctorId = 3 },
        new() { Id = 7, ClinicId = 4, DoctorId = 1 },
        new() { Id = 8, ClinicId = 5, DoctorId = 2 },
        new() { Id = 9, ClinicId = 6, DoctorId = 3 },
        new() { Id = 10, ClinicId = 7, DoctorId = 1 },
        new() { Id = 11, ClinicId = 8, DoctorId = 2 }
    };

    // Mock Appointments — Upcoming, Completed, Cancelled
    private static readonly List<Appointment> StaticAppointments = new()
    {
        new() { Id = 1, UserId = 1, DoctorId = 1, DoctorName = "Dr Emily Carter", Department = "General Care", ClinicName = "Melbourne Central Medical", DateText = "2026-06-22", TimeText = "10:30 AM", Reason = "Annual General Checkup", Status = "Upcoming", TotalFee = 85.00 },
        new() { Id = 2, UserId = 1, DoctorId = 2, DoctorName = "Dr Michael Brown", Department = "Heart Clinic", ClinicName = "City Health Clinic", DateText = "2026-06-10", TimeText = "02:00 PM", Reason = "Follow-up ECG consultation", Status = "Completed", TotalFee = 150.00 },
        new() { Id = 3, UserId = 1, DoctorId = 3, DoctorName = "Dr Sarah Wilson", Department = "Skin Care", ClinicName = "Family Care Centre", DateText = "2026-06-05", TimeText = "11:30 AM", Reason = "Mole check cancelled by patient", Status = "Cancelled", TotalFee = 120.00 }
    };

    // Mock Documents — PDF Reports, Blood Tests, Prescriptions
    private static readonly List<MedicalDocument> StaticDocuments = new()
    {
        new() { Id = 1, UserId = 1, DocumentType = "Report", FileName = "general_checkup_report.pdf", FilePath = "local_cache/checkup.pdf", Notes = "Annual checkup summary from Dr Carter.", UploadedAt = DateTime.Now.AddDays(-4) },
        new() { Id = 2, UserId = 1, DocumentType = "Blood Test", FileName = "blood_test_june2026.pdf", FilePath = "local_cache/blood_test.pdf", Notes = "Full blood count, iron, vitamin D check.", UploadedAt = DateTime.Now.AddDays(-10) },
        new() { Id = 3, UserId = 1, DocumentType = "Prescription", FileName = "prescription_metformin.pdf", FilePath = "local_cache/prescription.pdf", Notes = "Metformin 500mg twice daily for 3 months.", UploadedAt = DateTime.Now.AddDays(-15) }
    };

    private static readonly List<EmailReminder> StaticEmailReminders = new();
    private static UserAccount? _currentUser;
    private static bool _isLoggedIn;

    private DatabaseService() { }

    public async Task<UserAccount> RegisterUserAsync(string fullName, string email, string phone, string dateOfBirth, string password)
    {
        _currentUser = new UserAccount
        {
            Id = 1,
            FullName = fullName,
            Email = email,
            Phone = phone,
            DateOfBirth = dateOfBirth,
            Role = email.Contains("admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Patient"
        };
        _isLoggedIn = true;
        Preferences.Set(LoggedInKey, true);
        Preferences.Set(CurrentUserKey, _currentUser.Id);
        Preferences.Set("medibook_onboarding_seen", true);
        return _currentUser;
    }

    public async Task<UserAccount?> LoginAsync(string email, string password)
    {
        bool isAdmin = email.Contains("admin", StringComparison.OrdinalIgnoreCase);
        _currentUser = new UserAccount
        {
            Id = isAdmin ? 2 : 1,
            FullName = isAdmin ? "Admin Principal" : "Akash Bhai",
            Email = email,
            Phone = isAdmin ? "+61 400 999 999" : "+61 400 123 456",
            DateOfBirth = isAdmin ? "01/01/1980" : "15/08/1995",
            Role = isAdmin ? "Admin" : "Patient"
        };
        _isLoggedIn = true;
        Preferences.Set(LoggedInKey, true);
        Preferences.Set(CurrentUserKey, _currentUser.Id);
        Preferences.Set("medibook_onboarding_seen", true);
        return _currentUser;
    }

    public async Task<UserAccount> SaveGoogleUserAsync(string fullName, string email, string googleSubject)
    {
        _currentUser = new UserAccount
        {
            Id = 1,
            FullName = fullName,
            Email = email,
            Phone = "+61 400 123 456",
            DateOfBirth = "15/08/1995",
            Role = "Patient",
            AuthProvider = "Google"
        };
        _isLoggedIn = true;
        Preferences.Set(LoggedInKey, true);
        Preferences.Set(CurrentUserKey, _currentUser.Id);
        Preferences.Set("medibook_onboarding_seen", true);
        return _currentUser;
    }

    public async Task<UserAccount?> GetCurrentUserAsync()
    {
        if (_isLoggedIn && _currentUser != null)
            return _currentUser;

        // Check if user was previously logged in
        if (Preferences.Get(LoggedInKey, false))
        {
            _currentUser = new UserAccount
            {
                Id = 1,
                FullName = "Akash Bhai",
                Email = "akash@medibook.com",
                Phone = "+61 400 123 456",
                DateOfBirth = "15/08/1995",
                Role = "Patient"
            };
            _isLoggedIn = true;
            return _currentUser;
        }

        return null;
    }

    public bool IsLoggedIn => _isLoggedIn || Preferences.Get(LoggedInKey, false);

    public void Logout()
    {
        _currentUser = null;
        _isLoggedIn = false;
        Preferences.Remove(CurrentUserKey);
        Preferences.Remove(LoggedInKey);
    }

    public async Task UpdateUserAsync(UserAccount user)
    {
        _currentUser = user;
        await Task.CompletedTask;
    }

    public async Task<List<Clinic>> GetClinicsAsync() => StaticClinics;

    public async Task<List<Doctor>> GetDoctorsAsync()
    {
        foreach (var doctor in StaticDoctors)
        {
            var mapping = StaticClinicDoctors.FirstOrDefault(cd => cd.DoctorId == doctor.Id);
            if (mapping != null)
            {
                var clinic = StaticClinics.FirstOrDefault(c => c.Id == mapping.ClinicId);
                if (clinic != null)
                {
                    doctor.ClinicName = clinic.Name;
                    doctor.DistanceText = clinic.DistanceText;
                }
            }
        }
        return StaticDoctors;
    }

    public async Task<Doctor?> GetDoctorAsync(int id)
    {
        var doctor = StaticDoctors.FirstOrDefault(d => d.Id == id);
        if (doctor != null)
        {
            var mapping = StaticClinicDoctors.FirstOrDefault(cd => cd.DoctorId == doctor.Id);
            if (mapping != null)
            {
                var clinic = StaticClinics.FirstOrDefault(c => c.Id == mapping.ClinicId);
                if (clinic != null)
                {
                    doctor.ClinicName = clinic.Name;
                    doctor.DistanceText = clinic.DistanceText;
                }
            }
        }
        return doctor;
    }

    public async Task<int> SaveAppointmentAsync(Appointment appointment)
    {
        appointment.Id = StaticAppointments.Count > 0 ? StaticAppointments.Max(a => a.Id) + 1 : 1;
        StaticAppointments.Add(appointment);
        return appointment.Id;
    }

    public async Task<Appointment?> GetAppointmentAsync(int id)
        => StaticAppointments.FirstOrDefault(a => a.Id == id);

    public async Task<List<Appointment>> GetAppointmentsForCurrentUserAsync()
        => StaticAppointments;

    public async Task<Appointment?> GetNextAppointmentAsync()
        => StaticAppointments.FirstOrDefault(a => a.Status == "Upcoming");

    public async Task SaveDocumentAsync(MedicalDocument document)
    {
        document.Id = StaticDocuments.Count > 0 ? StaticDocuments.Max(d => d.Id) + 1 : 1;
        StaticDocuments.Add(document);
        await Task.CompletedTask;
    }

    public async Task<List<MedicalDocument>> GetDocumentsForCurrentUserAsync()
        => StaticDocuments;

    public async Task DeleteDocumentAsync(int documentId)
    {
        var doc = StaticDocuments.FirstOrDefault(d => d.Id == documentId);
        if (doc != null)
        {
            StaticDocuments.Remove(doc);
        }
        await Task.CompletedTask;
    }

    public async Task SaveEmailReminderAsync(EmailReminder reminder)
    {
        if (reminder.Id == 0)
            reminder.Id = StaticEmailReminders.Count + 1;
        StaticEmailReminders.Add(reminder);
        await Task.CompletedTask;
    }

    public async Task<List<EmailReminder>> GetDueEmailRemindersAsync()
    {
        var today = DateTime.Today.ToString("yyyy-MM-dd");
        return StaticEmailReminders.Where(r => r.DueDateText == today && r.Status == "Pending").ToList();
    }

    public async Task MarkEmailReminderSentAsync(EmailReminder reminder)
    {
        var existing = StaticEmailReminders.FirstOrDefault(r => r.Id == reminder.Id);
        if (existing != null)
        {
            existing.Status = "Sent";
            existing.SentAt = DateTime.Now;
        }
        await Task.CompletedTask;
    }
}
