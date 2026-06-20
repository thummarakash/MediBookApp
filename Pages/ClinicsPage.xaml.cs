using MediBook.Models;
using MediBook.Services;
using Microsoft.Maui.Maps;

namespace MediBook.Pages;

public partial class ClinicsPage : ContentPage
{
    private List<Clinic> _clinics = new();
    private Clinic? _selectedClinic;
    private bool _isMapView = true;
    private bool _isLocationAvailable = false;
    private IDispatcherTimer? _locationCheckTimer;
    private string _searchText = "";
    private string _selectedCategory = "All";

    public ClinicsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _clinics = await DatabaseService.Instance.GetClinicsAsync();
        
        ApplyFilterAndSearch();

        // Check immediately on appearing
        await CheckAndCenterUserLocationAsync(requestIfNeeded: false);

        // Start periodic check timer (polls every 3 seconds to auto-hide map if user turns off GPS)
        _locationCheckTimer = Dispatcher.CreateTimer();
        _locationCheckTimer.Interval = TimeSpan.FromSeconds(3);
        _locationCheckTimer.Tick += async (s, e) =>
        {
            await CheckAndCenterUserLocationAsync(requestIfNeeded: false);
        };
        _locationCheckTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _locationCheckTimer?.Stop();
        _locationCheckTimer = null;
    }

    private async Task<bool> IsLocationServicesEnabledAsync()
    {
#if ANDROID
        try
        {
            var locationManager = (Android.Locations.LocationManager)Android.App.Application.Context.GetSystemService(Android.Content.Context.LocationService);
            return locationManager != null && (locationManager.IsProviderEnabled(Android.Locations.LocationManager.GpsProvider) || locationManager.IsProviderEnabled(Android.Locations.LocationManager.NetworkProvider));
        }
        catch
        {
            return false;
        }
#else
        return true;
#endif
    }

    private async Task CheckAndCenterUserLocationAsync(bool requestIfNeeded)
    {
        try
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted && requestIfNeeded)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            bool isGpsEnabled = await IsLocationServicesEnabledAsync();

            if (status == PermissionStatus.Granted && isGpsEnabled)
            {
                // If location wasn't available before, fetch location and center/sort
                if (!_isLocationAvailable)
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(5)));
                    if (location != null)
                    {
                        // Calculate driving distance (straight line * 1.3 scaling factor) to clinics
                        foreach (var clinic in _clinics)
                        {
                            var clinicLoc = new Location(clinic.Latitude, clinic.Longitude);
                            double straightLineDist = location.CalculateDistance(clinicLoc, DistanceUnits.Kilometers);
                            clinic.DistanceToUser = straightLineDist * 1.3;
                        }

                        // Sort clinics Closest first (najik to door)
                        _clinics = _clinics.OrderBy(c => c.DistanceToUser ?? double.MaxValue).ToList();

                        // Re-setup map pins based on sorted order
                        SetupMapPins();

                        // Force Collections to refresh their display
                        ClinicsCollection.ItemsSource = null;
                        ClinicsCollection.ItemsSource = _clinics;

                        _isLocationAvailable = true;
                        UpdateContentVisibility();

                        if (_selectedClinic == null && _clinics.Any())
                        {
                            SelectClinic(_clinics.First());
                        }
                        else if (_selectedClinic != null)
                        {
                            // Refresh display values on card
                            _selectedClinic = _clinics.FirstOrDefault(c => c.Id == _selectedClinic.Id) ?? _selectedClinic;
                            MapClinicName.Text = _selectedClinic.Name;
                            MapClinicDistance.Text = _selectedClinic.DistanceText;
                            MapClinicRating.Text = _selectedClinic.Rating;
                        }

                        ClinicsMap.MoveToRegion(MapSpan.FromCenterAndRadius(location, Distance.FromKilometers(1.5)));
                    }
                }
                return;
            }

            // If user clicked Enable Location but global GPS is off, open settings directly
            if (requestIfNeeded && !isGpsEnabled)
            {
#if ANDROID
                var intent = new Android.Content.Intent(Android.Provider.Settings.ActionLocationSourceSettings);
                intent.AddFlags(Android.Content.ActivityFlags.NewTask);
                Android.App.Application.Context.StartActivity(intent);
#else
                await DisplayAlert("GPS Required", "Please enable GPS/location services on your device.", "OK");
#endif
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error retrieving location: {ex.Message}");
        }

        // Location is disabled/off/denied
        _isLocationAvailable = false;
        UpdateContentVisibility();
    }

    private void UpdateContentVisibility()
    {
        if (_isLocationAvailable)
        {
            LocationPromptContainer.IsVisible = false;
            MapViewGrid.IsVisible = _isMapView;
            SelectedClinicCard.IsVisible = _isMapView;
            ClinicsCollection.IsVisible = !_isMapView;
        }
        else
        {
            LocationPromptContainer.IsVisible = true;
            MapViewGrid.IsVisible = false;
            SelectedClinicCard.IsVisible = false;
            ClinicsCollection.IsVisible = false;
        }
    }

    private async void OnEnableLocationClicked(object sender, EventArgs e)
    {
        await CheckAndCenterUserLocationAsync(requestIfNeeded: true);
    }

    private void SetupMapPins()
    {
        try
        {
            ClinicsMap.Pins.Clear();
            foreach (var clinic in _clinics)
            {
                var pin = new Microsoft.Maui.Controls.Maps.Pin
                {
                    Label = clinic.Name,
                    Address = clinic.Address,
                    Type = Microsoft.Maui.Controls.Maps.PinType.Place,
                    Location = new Location(clinic.Latitude, clinic.Longitude)
                };
                
                pin.MarkerClicked += (s, args) =>
                {
                    SelectClinic(clinic);
                };

                ClinicsMap.Pins.Add(pin);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up map pins: {ex.Message}");
        }
    }

    private void OnMapTabClicked(object sender, EventArgs e)
    {
        _isMapView = true;
        UpdateContentVisibility();

        MapTabBtn.BackgroundColor = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#155EEF"));
        MapTabBtn.TextColor = Colors.White;
        MapTabBtn.FontAttributes = FontAttributes.Bold;

        ListTabBtn.BackgroundColor = Color.FromArgb("#E6F1FB");
        ListTabBtn.TextColor = (Color)(Application.Current?.Resources["MutedText"] ?? Color.FromArgb("#718096"));
        ListTabBtn.FontAttributes = FontAttributes.None;
    }

    private void OnListTabClicked(object sender, EventArgs e)
    {
        _isMapView = false;
        UpdateContentVisibility();

        ListTabBtn.BackgroundColor = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#155EEF"));
        ListTabBtn.TextColor = Colors.White;
        ListTabBtn.FontAttributes = FontAttributes.Bold;

        MapTabBtn.BackgroundColor = Color.FromArgb("#E6F1FB");
        MapTabBtn.TextColor = (Color)(Application.Current?.Resources["MutedText"] ?? Color.FromArgb("#718096"));
        MapTabBtn.FontAttributes = FontAttributes.None;
    }

    private void SelectClinic(Clinic clinic)
    {
        _selectedClinic = clinic;
        MapClinicName.Text = clinic.Name;
        MapClinicDistance.Text = clinic.DistanceText;
        MapClinicRating.Text = clinic.Rating;

        try
        {
            ClinicsMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(clinic.Latitude, clinic.Longitude),
                Distance.FromKilometers(1.2)));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error moving map camera: {ex.Message}");
        }
    }

    private async void OnSelectedClinicCardTapped(object sender, EventArgs e)
    {
        if (_selectedClinic != null)
        {
            await Shell.Current.GoToAsync($"{nameof(ClinicDetailPage)}?clinicId={_selectedClinic.Id}");
        }
    }

    private async void OnClinicCardTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is Clinic clinic)
        {
            await Shell.Current.GoToAsync($"{nameof(ClinicDetailPage)}?clinicId={clinic.Id}");
        }
    }

    private void ApplyFilterAndSearch()
    {
        var filtered = _clinics.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(_searchText))
        {
            filtered = filtered.Where(c => c.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
                                        || c.Address.Contains(_searchText, StringComparison.OrdinalIgnoreCase));
        }
        if (_selectedCategory != "All")
        {
            filtered = filtered.Where(c => c.SpecialtiesList.Contains(_selectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        var list = filtered.ToList();
        ClinicsCollection.ItemsSource = list;
        SetupMapPinsWithList(list);

        if (_isLocationAvailable)
        {
            if (list.Any())
            {
                if (_selectedClinic == null || !list.Any(c => c.Id == _selectedClinic.Id))
                {
                    SelectClinic(list.First());
                }
            }
            else
            {
                _selectedClinic = null;
                SelectedClinicCard.IsVisible = false;
            }
        }
    }

    private void SetupMapPinsWithList(List<Clinic> list)
    {
        try
        {
            ClinicsMap.Pins.Clear();
            foreach (var clinic in list)
            {
                var pin = new Microsoft.Maui.Controls.Maps.Pin
                {
                    Label = clinic.Name,
                    Address = clinic.Address,
                    Type = Microsoft.Maui.Controls.Maps.PinType.Place,
                    Location = new Location(clinic.Latitude, clinic.Longitude)
                };
                
                pin.MarkerClicked += (s, args) =>
                {
                    SelectClinic(clinic);
                };

                ClinicsMap.Pins.Add(pin);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error setting up map pins: {ex.Message}");
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _searchText = e.NewTextValue ?? "";
        ApplyFilterAndSearch();
    }

    private void OnChipTapped(object sender, EventArgs e)
    {
        if (sender is Border border
            && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap
            && tap.CommandParameter is string category)
        {
            _selectedCategory = category;
            UpdateChipUI(category);
            ApplyFilterAndSearch();
        }
    }

    private void UpdateChipUI(string active)
    {
        var activeBlue = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#185FA5"));
        var lightBlue = Color.FromArgb("#E6F1FB");

        AllChip.BackgroundColor = active == "All" ? activeBlue : lightBlue;
        AllChipText.TextColor = active == "All" ? Colors.White : activeBlue;
        AllChipText.FontAttributes = active == "All" ? FontAttributes.Bold : FontAttributes.None;

        GeneralChip.BackgroundColor = active == "General Practice" ? activeBlue : lightBlue;
        GeneralChipText.TextColor = active == "General Practice" ? Colors.White : activeBlue;
        GeneralChipText.FontAttributes = active == "General Practice" ? FontAttributes.Bold : FontAttributes.None;

        CardiologyChip.BackgroundColor = active == "Cardiology" ? activeBlue : lightBlue;
        CardiologyChipText.TextColor = active == "Cardiology" ? Colors.White : activeBlue;
        CardiologyChipText.FontAttributes = active == "Cardiology" ? FontAttributes.Bold : FontAttributes.None;

        PediatricsChip.BackgroundColor = active == "Pediatrics" ? activeBlue : lightBlue;
        PediatricsChipText.TextColor = active == "Pediatrics" ? Colors.White : activeBlue;
        PediatricsChipText.FontAttributes = active == "Pediatrics" ? FontAttributes.Bold : FontAttributes.None;
    }
}
