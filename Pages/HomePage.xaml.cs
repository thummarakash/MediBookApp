using MediBook.Services;

namespace MediBook.Pages;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadDataAsync();
        await EmailNotificationService.Instance.ProcessDueReminderEmailsAsync();
    }

    private async Task LoadDataAsync()
    {
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        WelcomeLabel.Text = user == null ? "Welcome" : $"Hi, {user.FullName}";
        var next = await DatabaseService.Instance.GetNextAppointmentAsync();
        if (next != null)
        {
            NextDoctorLabel.Text = next.DoctorName;
            NextDateLabel.Text = $"{next.Department} • {next.DisplayDateTime}";
            NextStatusLabel.Text = next.EmailReminderQueued ? "Email reminder queued" : "Reminder not enabled";
        }
        else
        {
            NextDoctorLabel.Text = "No appointment booked";
            NextDateLabel.Text = "Book your first appointment today.";
            NextStatusLabel.Text = "Email reminder and document upload ready";
        }
    }

    private async void OnBookClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//doctors");
    private async void OnDoctorsClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("//doctors");
    private async void OnUploadClicked(object sender, EventArgs e) => await Shell.Current.GoToAsync(nameof(UploadDocumentPage));
    private async void OnCallClicked(object sender, EventArgs e)
    {
        try { NativeActionService.Instance.CallClinic("0290011234"); }
        catch (Exception ex) { await DisplayAlert("Phone", ex.Message, "OK"); }
    }
    private async void OnMapClicked(object sender, EventArgs e)
    {
        try { await NativeActionService.Instance.OpenClinicMapAsync(); }
        catch (Exception ex) { await DisplayAlert("Maps", ex.Message, "OK"); }
    }
}
