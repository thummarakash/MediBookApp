using MediBook.Helpers;
using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _vm = new();

    public HomePage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AnimationHelper.PageEntranceAsync(this);
        await _vm.SyncDashboardDataCommand.ExecuteAsync(null);
    }

    private async void OnBookTapped(object sender, EventArgs e)
    {
        if (sender is View v) await AnimationHelper.ButtonPressAsync(v);
        await Shell.Current.GoToAsync(nameof(BookAppointmentPage));
    }

    private async void OnDoctorsTapped(object sender, EventArgs e)
    {
        if (sender is View v) await AnimationHelper.ButtonPressAsync(v);
        await Shell.Current.GoToAsync(nameof(DoctorsPage));
    }

    private async void OnClinicsTapped(object sender, EventArgs e)
    {
        if (sender is View v) await AnimationHelper.ButtonPressAsync(v);
        await Shell.Current.GoToAsync("//clinics");
    }

    private async void OnDocumentsTapped(object sender, EventArgs e)
    {
        if (sender is View v) await AnimationHelper.ButtonPressAsync(v);
        await Shell.Current.GoToAsync("//documents");
    }

    private async void OnAppointmentsTapped(object sender, EventArgs e)
    {
        if (sender is View v) await AnimationHelper.ButtonPressAsync(v);
        await Shell.Current.GoToAsync("//appointments");
    }

    private async void OnProfileAvatarTapped(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(ProfilePage));
}
