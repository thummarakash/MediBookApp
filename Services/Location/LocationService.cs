using MediBook.Models;

namespace MediBook.Services.Location;

public class LocationService
{
    public static LocationService Instance { get; } = new();
    private LocationService() { }

    private Microsoft.Maui.Devices.Sensors.Location? _lastKnownLocation;

    public async Task<Microsoft.Maui.Devices.Sensors.Location?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    return null;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);
            if (location != null) _lastKnownLocation = location;
            return location;
        }
        catch (FeatureNotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine("[Location] GPS not supported on this device.");
            return null;
        }
        catch (PermissionException)
        {
            System.Diagnostics.Debug.WriteLine("[Location] Location permission denied.");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Location] Error: {ex.Message}");
            return null;
        }
    }

    public Microsoft.Maui.Devices.Sensors.Location? GetLastKnownLocation() => _lastKnownLocation;

    public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var from = new Microsoft.Maui.Devices.Sensors.Location(lat1, lon1);
        var to = new Microsoft.Maui.Devices.Sensors.Location(lat2, lon2);
        return from.CalculateDistance(to, DistanceUnits.Kilometers);
    }

    public async Task UpdateClinicDistancesAsync(List<Clinic> clinics)
    {
        var location = await GetCurrentLocationAsync() ?? _lastKnownLocation;
        if (location == null) return;

        foreach (var clinic in clinics)
        {
            clinic.DistanceToUser = CalculateDistanceKm(
                location.Latitude, location.Longitude,
                clinic.Latitude, clinic.Longitude);
        }
    }

    public List<Clinic> SortByDistance(List<Clinic> clinics)
        => clinics.OrderBy(c => c.DistanceToUser).ToList();

    public async Task OpenDirectionsAsync(double latitude, double longitude, string label)
    {
        try
        {
            var location = new Microsoft.Maui.Devices.Sensors.Location(latitude, longitude);
            var options = new MapLaunchOptions { Name = label, NavigationMode = NavigationMode.Driving };
            await Map.Default.OpenAsync(location, options);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Maps] Cannot open: {ex.Message}");
        }
    }
}
