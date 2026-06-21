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
        catch (Exception ex) { await ConfirmationPopupPage.ShowAsync(Navigation, "Phone Error", ex.Message, "icon_warning.svg"); }
    }

    private async void OnEmailClicked(object sender, EventArgs e)
    {
        try { await NativeActionService.Instance.ComposeEmailAsync("reception@medibookclinic.com", "MediBook patient enquiry", "Hello MediBook team,"); }
        catch (Exception ex) { await ConfirmationPopupPage.ShowAsync(Navigation, "Email Error", ex.Message, "icon_warning.svg"); }
    }

    private async void OnMapClicked(object sender, EventArgs e)
    {
        try { await NativeActionService.Instance.OpenClinicMapAsync(); }
        catch (Exception ex) { await ConfirmationPopupPage.ShowAsync(Navigation, "Maps Error", ex.Message, "icon_warning.svg"); }
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//profile");
    }
}
