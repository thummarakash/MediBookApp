using MediBook.Services;

namespace MediBook.Pages;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await Task.Delay(900);
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        await Shell.Current.GoToAsync(user == null ? "//login" : "//home");
    }
}
