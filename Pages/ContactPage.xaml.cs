using MediBook.Services;

namespace MediBook.Pages;

public partial class ContactPage : ContentPage
{
    public ContactPage()
    {
        InitializeComponent();
    }

    private async void OnCallClicked(object sender, EventArgs e)
    {
        try { NativeActionService.Instance.CallClinic("0290011234"); }
        catch (Exception phone_ex) { await DisplayAlert("Phone", phone_ex.Message, "OK"); }
    }

    private async void OnEmailClicked(object sender, EventArgs e)
    {
        try { await NativeActionService.Instance.ComposeEmailAsync("reception@medibookclinic.com", "MediBook patient enquiry", "Hello MediBook team,"); }
        catch (Exception email_ex) { await DisplayAlert("Email", email_ex.Message, "OK"); }
    }

    private async void OnMapClicked(object sender, EventArgs e)
    {
        try { await NativeActionService.Instance.OpenClinicMapAsync(); }
        catch (Exception map_ex) { await DisplayAlert("Maps", map_ex.Message, "OK"); }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//profile");
    }
}
