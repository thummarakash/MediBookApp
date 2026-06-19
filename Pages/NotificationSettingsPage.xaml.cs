namespace MediBook.Pages;

public partial class NotificationSettingsPage : ContentPage
{
    public NotificationSettingsPage()
    {
        InitializeComponent();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
