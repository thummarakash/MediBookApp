using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;
using MediBook.Pages;

namespace MediBook.ViewModels;

[QueryProperty(nameof(DoctorId), "doctorId")]
public partial class BookAppointmentViewModel : ObservableObject
{
    private List<Doctor> _allDoctors = new();

    [ObservableProperty]
    private string _doctorId = string.Empty;

    [ObservableProperty]
    private string _selectedSpecialty = string.Empty;

    [ObservableProperty]
    private Doctor? _selectedDoctor;

    [ObservableProperty]
    private string _selectedTime = string.Empty;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private string _reasonText = string.Empty;

    [ObservableProperty]
    private bool _isEmailReminderEnabled = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private int _currentStep = 1;

    [ObservableProperty]
    private string _stepDescription = "Step 1: Select Specialty";

    [ObservableProperty]
    private ObservableCollection<Doctor> _filteredDoctors = new();

    [ObservableProperty]
    private bool _isStep1Visible = true;

    [ObservableProperty]
    private bool _isStep2Visible;

    [ObservableProperty]
    private bool _isStep3Visible;

    [ObservableProperty]
    private bool _isStep4Visible;

    [ObservableProperty]
    private bool _isConfirmPopupVisible;

    [ObservableProperty]
    private string _summaryDateTimeText = string.Empty;

    partial void OnDoctorIdChanged(string value)
    {
        _ = LoadInitialDoctorAsync(value);
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterDoctors();
    }

    partial void OnSelectedDateChanged(DateTime value) => UpdateSummaryDateTime();
    partial void OnSelectedTimeChanged(string value) => UpdateSummaryDateTime();

    private void UpdateSummaryDateTime()
    {
        SummaryDateTimeText = $"{SelectedDate:dd MMM yyyy} at {SelectedTime}";
    }

    public async Task InitializeAsync()
    {
        _allDoctors = await DatabaseService.Instance.GetDoctorsAsync();
        if (int.TryParse(DoctorId, out var doctorId))
        {
            await LoadInitialDoctorAsync(DoctorId);
        }
        else
        {
            SetStep(1);
        }
    }

    private async Task LoadInitialDoctorAsync(string docId)
    {
        if (int.TryParse(docId, out var id))
        {
            var doc = await DatabaseService.Instance.GetDoctorAsync(id);
            if (doc != null)
            {
                SelectedDoctor = doc;
                SelectedSpecialty = doc.Specialty;
                SetStep(3);
            }
        }
    }

    public void SetStep(int step)
    {
        CurrentStep = step;
        IsStep1Visible = (step == 1);
        IsStep2Visible = (step == 2);
        IsStep3Visible = (step == 3);
        IsStep4Visible = (step == 4);

        switch (step)
        {
            case 1:
                StepDescription = "Step 1: Select Specialty";
                break;
            case 2:
                StepDescription = "Step 2: Select Doctor";
                LoadDoctorsForSpecialty();
                break;
            case 3:
                StepDescription = "Step 3: Select Date & Time";
                break;
            case 4:
                StepDescription = "Step 4: Confirm Booking";
                break;
        }
    }

    private void LoadDoctorsForSpecialty()
    {
        var filtered = _allDoctors.Where(d =>
            d.Specialty.Equals(SelectedSpecialty, StringComparison.OrdinalIgnoreCase) ||
            d.Department.Equals(SelectedSpecialty, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (!filtered.Any())
        {
            filtered = _allDoctors;
        }

        FilteredDoctors = new ObservableCollection<Doctor>(filtered);
        FilterDoctors();
    }

    private void FilterDoctors()
    {
        var text = SearchText?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            FilteredDoctors = new ObservableCollection<Doctor>(
                _allDoctors.Where(d => d.Specialty.Equals(SelectedSpecialty, StringComparison.OrdinalIgnoreCase) ||
                                       d.Department.Equals(SelectedSpecialty, StringComparison.OrdinalIgnoreCase)).ToList()
            );
            if (!FilteredDoctors.Any())
            {
                FilteredDoctors = new ObservableCollection<Doctor>(_allDoctors);
            }
        }
        else
        {
            var searchBase = _allDoctors.Where(d => d.Specialty.Equals(SelectedSpecialty, StringComparison.OrdinalIgnoreCase) ||
                                                   d.Department.Equals(SelectedSpecialty, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!searchBase.Any())
            {
                searchBase = _allDoctors;
            }
            FilteredDoctors = new ObservableCollection<Doctor>(
                searchBase.Where(d => d.Name.ToLowerInvariant().Contains(text)).ToList()
            );
        }
    }

    [RelayCommand]
    private void SelectSpecialty(string specialty)
    {
        SelectedSpecialty = specialty;
        SetStep(2);
    }

    [RelayCommand]
    private void SelectDoctor(Doctor doctor)
    {
        SelectedDoctor = doctor;
        SetStep(3);
    }

    [RelayCommand]
    private void SelectTimeSlot(string timeSlot)
    {
        SelectedTime = timeSlot;
        SetStep(4);
    }

    [RelayCommand]
    private async Task ConfirmBookingAsync()
    {
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user == null)
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Login Required", "Please login before booking an appointment.", "icon_warning.svg");
            await Shell.Current.GoToAsync("//login");
            return;
        }

        if (SelectedDoctor == null)
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Doctor Required", "Please select a doctor.", "icon_warning.svg");
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedTime))
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Time Required", "Please select a time slot.", "icon_warning.svg");
            return;
        }

        if (string.IsNullOrWhiteSpace(ReasonText))
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Reason Required", "Please enter a short reason for your visit.", "icon_warning.svg");
            return;
        }

        IsConfirmPopupVisible = true;
    }

    [RelayCommand]
    private void CancelBookingPopup()
    {
        IsConfirmPopupVisible = false;
    }

    [RelayCommand]
    private async Task ExecuteBookingAsync()
    {
        IsConfirmPopupVisible = false;
        var user = await DatabaseService.Instance.GetCurrentUserAsync();
        if (user == null || SelectedDoctor == null) return;

        var appointment = new Appointment
        {
            UserId = user.Id,
            DoctorId = SelectedDoctor.Id,
            DoctorName = SelectedDoctor.Name,
            Department = SelectedDoctor.Department,
            ClinicName = SelectedDoctor.ClinicName ?? "Melbourne Central Medical",
            DateText = $"{SelectedDate:yyyy-MM-dd}",
            TimeText = SelectedTime,
            Reason = ReasonText.Trim(),
            Status = "Upcoming",
            ReminderEnabled = IsEmailReminderEnabled,
            EmailReminderQueued = IsEmailReminderEnabled,
            CreatedAt = DateTime.Now,
            TotalFee = SelectedDoctor.FeePerAppointment
        };

        await DatabaseService.Instance.SaveAppointmentAsync(appointment);

        if (IsEmailReminderEnabled)
        {
            await EmailNotificationService.Instance.QueueAndSendAppointmentEmailAsync(user, appointment, SelectedDoctor);
        }

        await Shell.Current.GoToAsync($"{nameof(AppointmentConfirmationPage)}?appointmentId={appointment.Id}");
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        if (CurrentStep > 1)
        {
            SetStep(CurrentStep - 1);
        }
        else
        {
            DoctorId = string.Empty;
            SelectedDoctor = null;
            await Shell.Current.GoToAsync("..");
        }
    }
}
