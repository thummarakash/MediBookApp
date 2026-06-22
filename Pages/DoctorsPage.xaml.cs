using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class DoctorsPage : ContentPage
{
    private readonly DoctorsViewModel _vm = new();
    private bool _isAdmin = false;

    public DoctorsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var user = await Services.DatabaseService.Instance.GetCurrentUserAsync();
        _isAdmin = user?.Role == "Admin";
        await _vm.LoadDoctorsCommand.ExecuteAsync(null);
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchDoctorsCommand.Execute(e.NewTextValue);
    }

    private async void OnDoctorCardTapped(object sender, EventArgs e)
    {
        if (sender is Border border && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap && tap.CommandParameter is Models.Doctor doctor)
        {
            var dct = doctor;
            string spclty = string.IsNullOrEmpty(dct.Specialty) ? "-" : dct.Specialty;
            string dept = string.IsNullOrEmpty(dct.Department) ? "-" : dct.Department;
            string email = string.IsNullOrEmpty(dct.Email) ? "-" : dct.Email;
            string clnc = string.IsNullOrEmpty(dct.ClinicName) ? "-" : dct.ClinicName;

            string dctDetals = $"Email: {email}\n" +
                               $"Specialty: {spclty}\n" +
                               $"Department: {dept}\n" +
                               $"Clinic: {clnc}\n" +
                               $"Fee: ${dct.FeePerAppointment:F2} / {dct.SlotDurationMinutes} mins";

            if (_isAdmin)
            {
                await ConfirmationPopupPage.ShowAsync(
                    Navigation, 
                    dct.Name ?? "-", 
                    dctDetals, 
                    "icon_doctor.svg",
                    "Close"
                );
            }
            else
            {
                bool book = await ConfirmationPopupPage.ShowConfirmAsync(
                    Navigation, 
                    dct.Name ?? "-", 
                    dctDetals, 
                    "Book Appointment", 
                    "Close", 
                    "icon_doctor.svg"
                );
                if (book)
                {
                    await Shell.Current.GoToAsync($"{nameof(BookAppointmentPage)}?doctorId={dct.Id}");
                }
            }
        }
    }

    private void OnChipTapped(object sender, EventArgs e)
    {
        if (sender is Border border
            && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap
            && tap.CommandParameter is string category)
        {
            _vm.SelectSpecialtyCategoryCommand.Execute(category);
            UpdateChipUI(category);
        }
    }

    private void UpdateChipUI(string active)
    {
        var activeBlue = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#185FA5"));
        var lightBlue = Color.FromArgb("#E6F1FB");

        AllChip.BackgroundColor = active == "All" ? activeBlue : lightBlue;
        AllChipText.TextColor = active == "All" ? Colors.White : activeBlue;
        AllChipText.FontAttributes = active == "All" ? FontAttributes.Bold : FontAttributes.None;

        GeneralChip.BackgroundColor = active == "General" ? activeBlue : lightBlue;
        GeneralChipText.TextColor = active == "General" ? Colors.White : activeBlue;
        GeneralChipText.FontAttributes = active == "General" ? FontAttributes.Bold : FontAttributes.None;

        CardiologyChip.BackgroundColor = active == "Cardiology" ? activeBlue : lightBlue;
        CardiologyChipText.TextColor = active == "Cardiology" ? Colors.White : activeBlue;
        CardiologyChipText.FontAttributes = active == "Cardiology" ? FontAttributes.Bold : FontAttributes.None;

        PediatricsChip.BackgroundColor = active == "Pediatrics" ? activeBlue : lightBlue;
        PediatricsChipText.TextColor = active == "Pediatrics" ? Colors.White : activeBlue;
        PediatricsChipText.FontAttributes = active == "Pediatrics" ? FontAttributes.Bold : FontAttributes.None;
    }
}
