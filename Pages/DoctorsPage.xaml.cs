using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class DoctorsPage : ContentPage
{
    private readonly DoctorsViewModel _vm = new();

    public DoctorsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchCommand.Execute(e.NewTextValue);
    }

    private async void OnDoctorCardTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is Models.Doctor doctor)
        {
            await Shell.Current.GoToAsync($"{nameof(BookAppointmentPage)}?doctorId={doctor.Id}");
        }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//home");
    }
}
