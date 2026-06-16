using MediBook.Models;
using MediBook.Services;

namespace MediBook.Pages;

public partial class DoctorsPage : ContentPage
{
    private List<Doctor> _doctors = new();

    public DoctorsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _doctors = await DatabaseService.Instance.GetDoctorsAsync();
        DoctorsCollection.ItemsSource = _doctors;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var text = e.NewTextValue?.Trim().ToLowerInvariant() ?? string.Empty;
        DoctorsCollection.ItemsSource = string.IsNullOrWhiteSpace(text)
            ? _doctors
            : _doctors.Where(d => d.Name.ToLowerInvariant().Contains(text)
                               || d.Department.ToLowerInvariant().Contains(text)
                               || d.Specialty.ToLowerInvariant().Contains(text)).ToList();
    }

    private async void OnBookDoctorClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is Doctor doctor)
        {
            await Shell.Current.GoToAsync($"{nameof(BookAppointmentPage)}?doctorId={doctor.Id}");
        }
    }
}
