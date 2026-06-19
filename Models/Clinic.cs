using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MediBook.Models;

public class Clinic : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }

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
        : (Id == 1 ? "0.4 km away" : Id == 2 ? "1.2 km away" : "2.5 km away");

    public string Rating => Id == 1 ? "4.8" : Id == 2 ? "4.5" : "4.2";
    public string RatingCount => Id == 1 ? "234" : Id == 2 ? "118" : "64";
    public string Phone => Id == 1 ? "+61 3 9000 0000" : Id == 2 ? "+61 3 9876 5432" : "+61 3 5555 4444";
    public string SpecialtiesList => Id == 1 ? "General Practice, Cardiology, Dermatology" : Id == 2 ? "General Practice, Mental Health" : "General Practice, Physiotherapy";
    public List<string> Specialties => SpecialtiesList.Split(',').Select(s => s.Trim()).ToList();
    public string OpeningHoursMonFri => Id == 1 ? "Mon–Fri: 8AM–8PM" : "Mon–Fri: 9AM–6PM";
    public string OpeningHoursSatSun => Id == 1 ? "Sat: 9AM–5PM • Sun: 10AM–2PM" : "Sat: 9AM–1PM • Sun: Closed";
    public string Status => "Open";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string CreatedBy { get; set; } = "System";
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public string UpdatedBy { get; set; } = "System";
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
