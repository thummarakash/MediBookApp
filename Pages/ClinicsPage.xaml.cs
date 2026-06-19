using MediBook.Models;
using MediBook.Services;

namespace MediBook.Pages;

public partial class ClinicsPage : ContentPage
{
    private List<Clinic> _clinics = new();
    private Clinic? _selectedClinic;

    public ClinicsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _clinics = await DatabaseService.Instance.GetClinicsAsync();
        ClinicsCollection.ItemsSource = _clinics;
        
        if (_clinics.Any())
        {
            SelectClinic(_clinics.First());
        }
    }

    private void OnMapTabClicked(object sender, EventArgs e)
    {
        MapViewGrid.IsVisible = true;
        ClinicsCollection.IsVisible = false;

        MapTabBtn.BackgroundColor = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#155EEF"));
        MapTabBtn.TextColor = Colors.White;
        MapTabBtn.FontAttributes = FontAttributes.Bold;

        ListTabBtn.BackgroundColor = Color.FromArgb("#E6F1FB");
        ListTabBtn.TextColor = (Color)(Application.Current?.Resources["MutedText"] ?? Color.FromArgb("#718096"));
        ListTabBtn.FontAttributes = FontAttributes.None;
    }

    private void OnListTabClicked(object sender, EventArgs e)
    {
        MapViewGrid.IsVisible = false;
        ClinicsCollection.IsVisible = true;

        ListTabBtn.BackgroundColor = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#155EEF"));
        ListTabBtn.TextColor = Colors.White;
        ListTabBtn.FontAttributes = FontAttributes.Bold;

        MapTabBtn.BackgroundColor = Color.FromArgb("#E6F1FB");
        MapTabBtn.TextColor = (Color)(Application.Current?.Resources["MutedText"] ?? Color.FromArgb("#718096"));
        MapTabBtn.FontAttributes = FontAttributes.None;
    }

    private void OnMapPinClicked(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is string pinIdStr && int.TryParse(pinIdStr, out var pinId))
        {
            var clinic = _clinics.FirstOrDefault(c => c.Id == pinId);
            if (clinic != null)
            {
                SelectClinic(clinic);
            }
        }
    }

    private void SelectClinic(Clinic clinic)
    {
        _selectedClinic = clinic;
        MapClinicName.Text = clinic.Name;
        MapClinicDistance.Text = clinic.DistanceText;
        MapClinicRating.Text = clinic.Rating;

        // Reset pin colors and scale
        Pin1Img.Source = "icon_pin_blue.png";
        Pin2Img.Source = "icon_pin_blue.png";
        Pin3Img.Source = "icon_pin_blue.png";

        Pin1.Scale = 1.0;
        Pin2.Scale = 1.0;
        Pin3.Scale = 1.0;

        // Highlight active pin
        if (clinic.Id == 1)
        {
            Pin1Img.Source = "icon_pin_red.png";
            Pin1.Scale = 1.25;
        }
        else if (clinic.Id == 2)
        {
            Pin2Img.Source = "icon_pin_red.png";
            Pin2.Scale = 1.25;
        }
        else if (clinic.Id == 3)
        {
            Pin3Img.Source = "icon_pin_red.png";
            Pin3.Scale = 1.25;
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
}
