using MediBook.Models;

namespace MediBook.Services.Location;

public class LocationService
{
    public static LocationService Instance { get; } = new();
    private LocationService() { }

    private Microsoft.Maui.Devices.Sensors.Location? _lastLoc;

    public async Task<Microsoft.Maui.Devices.Sensors.Location?> GetCurrentLocationAsync()
    {
        try
        {
            var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permissionStatus != PermissionStatus.Granted)
            {
                permissionStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (permissionStatus != PermissionStatus.Granted)
                    return null;
            }

            var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);

            if (location != null)
                _lastLoc = location;

            return location;
        }
        catch (FeatureNotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] GPS is not supported on this device.");
            return null;
        }
        catch (PermissionException)
        {
            System.Diagnostics.Debug.WriteLine("[LocationService] Location permission denied.");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocationService] GetCurrentLocationAsync failed: {ex.Message}");
            return null;
        }
    }

    public Microsoft.Maui.Devices.Sensors.Location? GetLastKnownLocation() => _lastLoc;

    public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var from = new Microsoft.Maui.Devices.Sensors.Location(lat1, lon1);
        var to = new Microsoft.Maui.Devices.Sensors.Location(lat2, lon2);
        return from.CalculateDistance(to, DistanceUnits.Kilometers);
    }

    public async Task UpdateClinicDistancesAsync(List<Clinic> clinics)
    {
        var location = await GetCurrentLocationAsync() ?? _lastLoc;
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
            var targetLocation = new Microsoft.Maui.Devices.Sensors.Location(latitude, longitude);
            var mapOptions = new MapLaunchOptions { Name = label, NavigationMode = NavigationMode.Driving };
            await Map.Default.OpenAsync(targetLocation, mapOptions);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocationService] OpenDirectionsAsync failed: {ex.Message}");
        }
    }
}
