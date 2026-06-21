using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediBook.Models;

public class Clinic : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string FirestoreId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Stored fields from Firestore (override computed properties when set)
    public string? Phone { get; set; }
    public string? OpeningHoursMonFri { get; set; }
    public string? OpeningHoursSatSun { get; set; }
    public string? Status { get; set; }

    private double? _distanceToUser;
    public double? DistanceToUser
    {
        get => _distanceToUser;
        set
        {
            if (_distanceToUser != value)
            {
                _distanceToUser = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DistanceText));
            }
        }
    }

    public string DistanceText => DistanceToUser.HasValue
        ? $"{DistanceToUser.Value:F1} km away"
        : "- km";

    public string Rating => Id == 1 ? "4.8" : Id == 2 ? "4.5" : "4.2";
    public string RatingCount => Id == 1 ? "234" : Id == 2 ? "118" : "64";
    public string SpecialtiesList => "General Practice, Cardiology";
    public List<string> Specialties => SpecialtiesList.Split(',').Select(s => s.Trim()).ToList();

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "System";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
