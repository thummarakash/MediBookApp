using MediBook.Models;
using MediBook.Services;
using Microsoft.Maui.Controls.Shapes;

namespace MediBook.Pages;

[QueryProperty(nameof(ClinicId), "clinicId")]
public partial class ClinicDetailPage : ContentPage
{
    private Clinic? _clinic;
    public string ClinicId { get; set; } = string.Empty;

    public ClinicDetailPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (int.TryParse(ClinicId, out var id))
        {
            _clinic = (await DatabaseService.Instance.GetClinicsAsync()).FirstOrDefault(c => c.Id == id);
        }

        if (_clinic != null)
        {
            ClinicNameLabel.Text = _clinic.Name;
            ClinicAddressLabel.Text = _clinic.Address;
            ClinicPhoneLabel.Text = _clinic.Phone;
            ClinicRatingLabel.Text = $"{_clinic.Rating} ({_clinic.RatingCount} reviews)";
            HoursMonFriLabel.Text = _clinic.OpeningHoursMonFri;
            HoursSatSunLabel.Text = _clinic.OpeningHoursSatSun;

            SpecialtiesFlex.Children.Clear();
            foreach (var spec in _clinic.Specialties)
            {
                var border = new Border
                {
                    BackgroundColor = Color.FromArgb("#E6F1FB"),
                    StrokeThickness = 0,
                    Padding = new Thickness(12, 6),
                    Margin = new Thickness(0, 4, 8, 4),
                };
                border.StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) };

                var label = new Label
                {
                    Text = spec,
                    TextColor = Color.FromArgb("#0C447C"),
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold
                };
                border.Content = label;
                SpecialtiesFlex.Children.Add(border);
            }
        }
    }

    private async void OnDirectionsClicked(object sender, EventArgs e)
    {
        if (_clinic != null)
        {
            try
            {
                var options = new MapLaunchOptions { Name = _clinic.Name };
                await Map.OpenAsync(_clinic.Latitude, _clinic.Longitude, options);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Maps Error", ex.Message, "OK");
            }
        }
    }

    // FIX: was using "//BookAppointmentPage" which crashes because it's a registered route, not a global route
    private async void OnBookClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(BookAppointmentPage));
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
