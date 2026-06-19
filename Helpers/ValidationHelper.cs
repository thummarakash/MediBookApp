using System.Text.RegularExpressions;

namespace MediBook.Helpers;

public static class ValidationHelper
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PhoneRegex = new(
        @"^\+?[\d\s\-\(\)]{7,15}$",
        RegexOptions.Compiled);

    public static bool IsValidEmail(string? email)
        => !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email.Trim());

    public static bool IsValidPassword(string? password)
        => !string.IsNullOrWhiteSpace(password) && password.Length >= 6;

    public static bool IsValidPhone(string? phone)
        => string.IsNullOrWhiteSpace(phone) || PhoneRegex.IsMatch(phone.Trim());

    public static bool IsValidName(string? name)
        => !string.IsNullOrWhiteSpace(name) && name.Trim().Length >= 2;

    public static string? ValidateRegistration(string fullName, string email, string password, string confirmPassword)
    {
        if (!IsValidName(fullName))
            return "Please enter your full name (at least 2 characters).";
        if (!IsValidEmail(email))
            return "Please enter a valid email address.";
        if (!IsValidPassword(password))
            return "Password must be at least 6 characters.";
        if (password != confirmPassword)
            return "Passwords do not match.";
        return null;
    }

    public static string? ValidateLogin(string email, string password)
    {
        if (!IsValidEmail(email))
            return "Please enter a valid email address.";
        if (string.IsNullOrWhiteSpace(password))
            return "Please enter your password.";
        return null;
    }

    public static string SanitizeInput(string? input)
        => string.IsNullOrWhiteSpace(input) ? string.Empty : input.Trim();
}
