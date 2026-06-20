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
            var loc_permission = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (loc_permission != PermissionStatus.Granted)
            {
                loc_permission = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (loc_permission != PermissionStatus.Granted)
                    return null;
            }

            // Set up a standard geolocation query request
            var request_params = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
            var retrieved_location = await Geolocation.Default.GetLocationAsync(request_params);
            
            if (retrieved_location != null) 
                _lastLoc = retrieved_location;
                
            return retrieved_location;
        }
        catch (FeatureNotSupportedException)
        {
            System.Diagnostics.Debug.WriteLine("[LocSvc] GPS is not supported on this specific device hardware.");
            return null;
        }
        catch (PermissionException)
        {
            System.Diagnostics.Debug.WriteLine("[LocSvc] Location permission was denied by user.");
            return null;
        }
        catch (Exception location_err)
        {
            System.Diagnostics.Debug.WriteLine($"[LocSvc] Fetching location failed: {location_err.Message}");
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

    public async Task UpdateClinicDistancesAsync(List<Clinic> clinics_list)
    {
        var retrieved_location = await GetCurrentLocationAsync() ?? _lastLoc;
        if (retrieved_location == null) return;

        foreach (var clinic in clinics_list)
        {
            clinic.DistanceToUser = CalculateDistanceKm(
                retrieved_location.Latitude, retrieved_location.Longitude,
                clinic.Latitude, clinic.Longitude);
        }
    }

    public List<Clinic> SortByDistance(List<Clinic> clinics_list)
        => clinics_list.OrderBy(c => c.DistanceToUser).ToList();

    public async Task OpenDirectionsAsync(double latitude, double longitude, string label)
    {
        try
        {
            var target_loc = new Microsoft.Maui.Devices.Sensors.Location(latitude, longitude);
            var map_opts = new MapLaunchOptions { Name = label, NavigationMode = NavigationMode.Driving };
            await Map.Default.OpenAsync(target_loc, map_opts);
        }
        catch (Exception map_ex)
        {
            System.Diagnostics.Debug.WriteLine($"[LocSvc] Cannot open navigation maps app: {map_ex.Message}");
        }
    }
}
