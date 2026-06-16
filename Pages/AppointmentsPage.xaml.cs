using MediBook.Services;

namespace MediBook.Pages;

public partial class AppointmentsPage : ContentPage
{
    public AppointmentsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        AppointmentsCollection.ItemsSource = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
    }

    private async void OnBookClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//doctors");
    }
}
