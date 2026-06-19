namespace MediBook.Configuration;

public static class AppConfig
{
    // Firebase Project
    public const string FirebaseProjectId = "medibook-d7710";
    public const string FirebaseWebApiKey = "AIzaSyBaMNysELXmRnvHYYA-fODhd0_qh6uIZr4";
    public const string FirebaseStorageBucket = "medibook-d7710.firebasestorage.app";

    // Firebase REST API base URLs
    public const string FirebaseAuthBaseUrl = "https://identitytoolkit.googleapis.com/v1";
    public const string FirebaseTokenRefreshUrl = "https://securetoken.googleapis.com/v1/token";
    public const string FirestoreBaseUrl = $"https://firestore.googleapis.com/v1/projects/{FirebaseProjectId}/databases/(default)/documents";
    public const string FirebaseStorageBaseUrl = $"https://firebasestorage.googleapis.com/v0/b/{FirebaseStorageBucket}/o";

    // Google OAuth
    public const string GoogleWebClientId = "735083808889-noubknkvaj40e8npvk59e9om4250502p.apps.googleusercontent.com";

    // Firestore collection names
    public static class Collections
    {
        public const string Users = "users";
        public const string Appointments = "appointments";
        public const string Clinics = "clinics";
        public const string Doctors = "doctors";
        public const string ClinicDoctors = "clinic_doctors";
        public const string MedicalDocuments = "medical_documents";
        public const string EmailReminders = "email_reminders";
        public const string Notifications = "notifications";
    }

    // Firebase Storage folder paths
    public static class StoragePaths
    {
        public const string MedicalDocuments = "medical_documents";
        public const string ProfileAvatars = "profile_avatars";
        public const string Prescriptions = "prescriptions";
    }

    // SecureStorage keys
    public static class SecureKeys
    {
        public const string IdToken = "medibook_id_token";
        public const string RefreshToken = "medibook_refresh_token";
        public const string UserId = "medibook_user_id";
        public const string UserEmail = "medibook_user_email";
        public const string UserRole = "medibook_user_role";
        public const string TokenExpiry = "medibook_token_expiry";
    }

    // Preferences keys
    public static class PrefKeys
    {
        public const string LoggedIn = "medibook_logged_in";
        public const string OnboardingSeen = "medibook_onboarding_seen";
        public const string BiometricEnabled = "medibook_biometric_enabled";
        public const string PinEnabled = "medibook_pin_enabled";
        public const string PinValue = "medibook_pin_value";
        public const string FcmToken = "medibook_fcm_token";
    }

    // SMTP (Gmail)
    public static class Smtp
    {
        public const string Host = "smtp.gmail.com";
        public const int Port = 587;
        public const string SenderAddress = "akashthummarau@gmail.com";
        public const string SenderName = "MediBook";
        // App Password — remove spaces for use
        public const string AppPassword = "dnjnhldeilkmvdem";
    }

    // App behaviour
    public const int TokenRefreshBufferMinutes = 5;
    public const int MaxImageSizeKb = 1024;
    public const int HttpTimeoutSeconds = 30;
}
