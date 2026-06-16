using SQLite;

namespace MediBook.Models;

public class UserAccount
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string DateOfBirth { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string AuthProvider { get; set; } = "Local";
    public string GoogleSubject { get; set; } = string.Empty;
    public string AvatarColor { get; set; } = "#155EEF";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastLoginAt { get; set; } = DateTime.Now;
}
