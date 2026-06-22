using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class ManageClinicsPage : ContentPage
{
    private readonly ManageClinicsViewModel _vm = new();

    public ManageClinicsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadClinicsCommand.ExecuteAsync(null);

        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync() ?? 
                           await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Low, TimeSpan.FromSeconds(2)));
            if (location != null)
            {
                LocationPickerMap.MoveToRegion(Microsoft.Maui.Maps.MapSpan.FromCenterAndRadius(location, Microsoft.Maui.Maps.Distance.FromKilometers(3)));
            }
        }
        catch {}
    }

    private void OnMapClicked(object sender, Microsoft.Maui.Controls.Maps.MapClickedEventArgs e)
    {
        double lat = e.Location.Latitude;
        double lon = e.Location.Longitude;

        _vm.ClinicLatitude = lat.ToString("F6");
        _vm.ClinicLongitude = lon.ToString("F6");

        LocationPickerMap.Pins.Clear();
        LocationPickerMap.Pins.Add(new Microsoft.Maui.Controls.Maps.Pin
        {
            Label = "Selected Location",
            Type = Microsoft.Maui.Controls.Maps.PinType.Place,
            Location = e.Location
        });

        Task.Run(async () =>
        {
            try
            {
                var placemarks = await Geocoding.Default.GetPlacemarksAsync(lat, lon);
                var placemark = placemarks?.FirstOrDefault();
                if (placemark != null)
                {
                    string address = $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, {placemark.Locality}, {placemark.AdminArea}";
                    address = address.Replace("  ", " ").Trim().TrimStart(',').Trim();
                    if (!string.IsNullOrEmpty(address))
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            _vm.ClinicAddress = address;
                        });
                    }
                }
            }
            catch {}
        });
    }

    private async void OnMapSearchClicked(object sender, EventArgs e)
    {
        string query = MapSearchEntry.Text;
        if (string.IsNullOrWhiteSpace(query)) return;

        try
        {
            var locations = await Geocoding.Default.GetLocationsAsync(query);
            var loc = locations?.FirstOrDefault();
            if (loc != null)
            {
                LocationPickerMap.MoveToRegion(Microsoft.Maui.Maps.MapSpan.FromCenterAndRadius(loc, Microsoft.Maui.Maps.Distance.FromKilometers(1.5)));

                _vm.ClinicLatitude = loc.Latitude.ToString("F6");
                _vm.ClinicLongitude = loc.Longitude.ToString("F6");

                LocationPickerMap.Pins.Clear();
                LocationPickerMap.Pins.Add(new Microsoft.Maui.Controls.Maps.Pin
                {
                    Label = query,
                    Type = Microsoft.Maui.Controls.Maps.PinType.Place,
                    Location = loc
                });

                try
                {
                    var placemarks = await Geocoding.Default.GetPlacemarksAsync(loc.Latitude, loc.Longitude);
                    var placemark = placemarks?.FirstOrDefault();
                    if (placemark != null)
                    {
                        string address = $"{placemark.Thoroughfare} {placemark.SubThoroughfare}, {placemark.Locality}, {placemark.AdminArea}";
                        address = address.Replace("  ", " ").Trim().TrimStart(',').Trim();
                        if (!string.IsNullOrEmpty(address))
                        {
                            _vm.ClinicAddress = address;
                        }
                        else
                        {
                            _vm.ClinicAddress = query;
                        }
                    }
                    else
                    {
                        _vm.ClinicAddress = query;
                    }
                }
                catch
                {
                    _vm.ClinicAddress = query;
                }
            }
            else
            {
                await DisplayAlert("Not Found", "Location not found on map.", "OK");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ManageClinics] map search failed: {ex.Message}");
        }
    }
}
