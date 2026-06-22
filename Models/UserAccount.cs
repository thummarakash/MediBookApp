namespace MediBook.Models;

public class UserAccount
{
    // local integer ID for backward compatibility with existing SQLite schema
    public int Id { get; set; }

    // Firebase UID — also the Firestore document ID for this user
    public string FirestoreId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = "Local";
    public string GoogleSubject { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#155EEF";
    public string Role { get; set; } = "Patient";
    public string FCMToken { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
    public double AvatarScale { get; set; } = 1.0;
    public double AvatarX { get; set; } = 0.0;
    public double AvatarY { get; set; } = 0.0;
    public double AvatarRotation { get; set; } = 0.0;
    public bool NotificationsEnabled { get; set; } = true;
    public bool LocationPermissionGranted { get; set; } = false;
    public bool BiometricEnabled { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "System";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public DateTime LastLoginAt { get; set; } = DateTime.Now;

    public string Initials
    {
        get
        {
            var parts = FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            // single-word name — just first letter; "US" is the safe fallback for empty names
            return parts.Length == 1 ? $"{parts[0][0]}".ToUpper() : "US";
        }
    }
}
