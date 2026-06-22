using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Repositories;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class ManageClinicsViewModel : ObservableObject
{
    private Clinic? _editingClinic;

    [ObservableProperty] string formTitle = "Add New Clinic";
    [ObservableProperty] string saveButtonText = "Save Clinic";
    [ObservableProperty] bool isEditMode;
    [ObservableProperty] string clinicName = string.Empty;
    [ObservableProperty] string clinicAddress = string.Empty;
    [ObservableProperty] string clinicLatitude = string.Empty;
    [ObservableProperty] string clinicLongitude = string.Empty;
    [ObservableProperty] ObservableCollection<Clinic> clinics = new();
    [ObservableProperty] ObservableCollection<Doctor> availableDoctors = new();
    [ObservableProperty] Doctor? selectedDoctor;
    [ObservableProperty] bool isLoading;
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(IsFormHidden))]
    bool isFormVisible;

    [ObservableProperty] bool mondayIsOpen = true;
    [ObservableProperty] TimeSpan mondayOpenTime = new TimeSpan(9, 0, 0);
    [ObservableProperty] TimeSpan mondayCloseTime = new TimeSpan(17, 0, 0);

    [ObservableProperty] bool tuesdayIsOpen = true;
    [ObservableProperty] TimeSpan tuesdayOpenTime = new TimeSpan(9, 0, 0);
    [ObservableProperty] TimeSpan tuesdayCloseTime = new TimeSpan(17, 0, 0);

    [ObservableProperty] bool wednesdayIsOpen = true;
    [ObservableProperty] TimeSpan wednesdayOpenTime = new TimeSpan(9, 0, 0);
    [ObservableProperty] TimeSpan wednesdayCloseTime = new TimeSpan(17, 0, 0);

    [ObservableProperty] bool thursdayIsOpen = true;
    [ObservableProperty] TimeSpan thursdayOpenTime = new TimeSpan(9, 0, 0);
    [ObservableProperty] TimeSpan thursdayCloseTime = new TimeSpan(17, 0, 0);

    [ObservableProperty] bool fridayIsOpen = true;
    [ObservableProperty] TimeSpan fridayOpenTime = new TimeSpan(9, 0, 0);
    [ObservableProperty] TimeSpan fridayCloseTime = new TimeSpan(17, 0, 0);

    [ObservableProperty] bool saturdayIsOpen = false;
    [ObservableProperty] TimeSpan saturdayOpenTime = new TimeSpan(9, 0, 0);
    [ObservableProperty] TimeSpan saturdayCloseTime = new TimeSpan(17, 0, 0);

    [ObservableProperty] bool sundayIsOpen = false;
    [ObservableProperty] TimeSpan sundayOpenTime = new TimeSpan(9, 0, 0);
    [ObservableProperty] TimeSpan sundayCloseTime = new TimeSpan(17, 0, 0);

    private void LoadWeeklySchedule(WeeklySchedule schedule)
    {
        MondayIsOpen = schedule.Monday.IsOpen;
        MondayOpenTime = ParseTime(schedule.Monday.OpenTime);
        MondayCloseTime = ParseTime(schedule.Monday.CloseTime);

        TuesdayIsOpen = schedule.Tuesday.IsOpen;
        TuesdayOpenTime = ParseTime(schedule.Tuesday.OpenTime);
        TuesdayCloseTime = ParseTime(schedule.Tuesday.CloseTime);

        WednesdayIsOpen = schedule.Wednesday.IsOpen;
        WednesdayOpenTime = ParseTime(schedule.Wednesday.OpenTime);
        WednesdayCloseTime = ParseTime(schedule.Wednesday.CloseTime);

        ThursdayIsOpen = schedule.Thursday.IsOpen;
        ThursdayOpenTime = ParseTime(schedule.Thursday.OpenTime);
        ThursdayCloseTime = ParseTime(schedule.Thursday.CloseTime);

        FridayIsOpen = schedule.Friday.IsOpen;
        FridayOpenTime = ParseTime(schedule.Friday.OpenTime);
        FridayCloseTime = ParseTime(schedule.Friday.CloseTime);

        SaturdayIsOpen = schedule.Saturday.IsOpen;
        SaturdayOpenTime = ParseTime(schedule.Saturday.OpenTime);
        SaturdayCloseTime = ParseTime(schedule.Saturday.CloseTime);

        SundayIsOpen = schedule.Sunday.IsOpen;
        SundayOpenTime = ParseTime(schedule.Sunday.OpenTime);
        SundayCloseTime = ParseTime(schedule.Sunday.CloseTime);
    }

    private WeeklySchedule GetWeeklyScheduleFromUi()
    {
        return new WeeklySchedule
        {
            Monday = new DaySchedule { IsOpen = MondayIsOpen, OpenTime = FormatTime(MondayOpenTime), CloseTime = FormatTime(MondayCloseTime) },
            Tuesday = new DaySchedule { IsOpen = TuesdayIsOpen, OpenTime = FormatTime(TuesdayOpenTime), CloseTime = FormatTime(TuesdayCloseTime) },
            Wednesday = new DaySchedule { IsOpen = WednesdayIsOpen, OpenTime = FormatTime(WednesdayOpenTime), CloseTime = FormatTime(WednesdayCloseTime) },
            Thursday = new DaySchedule { IsOpen = ThursdayIsOpen, OpenTime = FormatTime(ThursdayOpenTime), CloseTime = FormatTime(ThursdayCloseTime) },
            Friday = new DaySchedule { IsOpen = FridayIsOpen, OpenTime = FormatTime(FridayOpenTime), CloseTime = FormatTime(FridayCloseTime) },
            Saturday = new DaySchedule { IsOpen = SaturdayIsOpen, OpenTime = FormatTime(SaturdayOpenTime), CloseTime = FormatTime(SaturdayCloseTime) },
            Sunday = new DaySchedule { IsOpen = SundayIsOpen, OpenTime = FormatTime(SundayOpenTime), CloseTime = FormatTime(SundayCloseTime) }
        };
    }

    private TimeSpan ParseTime(string timeStr)
    {
        if (DateTime.TryParse(timeStr, out var dt))
        {
            return dt.TimeOfDay;
        }
        return new TimeSpan(9, 0, 0);
    }

    private string FormatTime(TimeSpan ts)
    {
        return DateTime.Today.Add(ts).ToString("hh:mm tt");
    }

    public bool IsFormHidden => !IsFormVisible;

    private System.Collections.Generic.List<Doctor> _allDoctors = new();

    private void UpdateAvailableDoctors()
    {
        string currentClinicId = _editingClinic?.FirestoreId ?? string.Empty;
        var filtered = new System.Collections.Generic.List<Doctor>();
        
        // Add "None" option so user can select no doctor
        filtered.Add(new Doctor { FirestoreId = string.Empty, Name = "None" });

        foreach (var d in _allDoctors)
        {
            if (string.IsNullOrEmpty(d.ClinicFirestoreId) || 
                (!string.IsNullOrEmpty(currentClinicId) && d.ClinicFirestoreId == currentClinicId))
            {
                filtered.Add(d);
            }
        }
        AvailableDoctors = new ObservableCollection<Doctor>(filtered);
    }

    [RelayCommand]
    void ShowForm()
    {
        _editingClinic = null;
        UpdateAvailableDoctors();
        SelectedDoctor = null;
        IsFormVisible = true;
    }

    [RelayCommand]
    async Task LoadClinicsAsync()
    {
        IsLoading = true;
        try
        {
            var list = await DatabaseService.Instance.GetClinicsAsync();
            Clinics = new ObservableCollection<Clinic>(list);

            var doctors = await DatabaseService.Instance.GetDoctorsAsync();
            _allDoctors = new System.Collections.Generic.List<Doctor>(doctors);
            UpdateAvailableDoctors();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void EditClinic(Clinic clinic)
    {
        if (clinic == null) return;
        _editingClinic = clinic;
        IsEditMode = true;
        IsFormVisible = true;
        FormTitle = "Edit Clinic";
        SaveButtonText = "Update Clinic";
        ClinicName = clinic.Name;
        ClinicAddress = clinic.Address;
        ClinicLatitude = clinic.Latitude.ToString("F6");
        ClinicLongitude = clinic.Longitude.ToString("F6");
        LoadWeeklySchedule(clinic.GetWeeklySchedule());
        UpdateAvailableDoctors();
        SelectedDoctor = AvailableDoctors.FirstOrDefault(d => !string.IsNullOrEmpty(clinic.FirestoreId) && d.ClinicFirestoreId == clinic.FirestoreId)
                         ?? AvailableDoctors.FirstOrDefault(d => string.IsNullOrEmpty(d.FirestoreId));
    }

    [RelayCommand]
    void CancelEdit()
    {
        _editingClinic = null;
        IsEditMode = false;
        IsFormVisible = false;
        FormTitle = "Add New Clinic";
        SaveButtonText = "Save Clinic";
        ClinicName = string.Empty;
        ClinicAddress = string.Empty;
        ClinicLatitude = string.Empty;
        ClinicLongitude = string.Empty;
        LoadWeeklySchedule(new WeeklySchedule());
        UpdateAvailableDoctors();
        SelectedDoctor = null;
    }

    [RelayCommand]
    async Task SaveClinicAsync()
    {
        if (string.IsNullOrWhiteSpace(ClinicName) || string.IsNullOrWhiteSpace(ClinicAddress))
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation,
                "Missing Details", "Please enter both name and address.", "icon_warning.svg");
            return;
        }

        double lat = -37.8136, lon = 144.9631;
        double parsedLat = 0;
        double parsedLon = 0;
        bool hasManualCoords = double.TryParse(ClinicLatitude, out parsedLat) &&
                               double.TryParse(ClinicLongitude, out parsedLon);

        if (hasManualCoords)
        {
            lat = parsedLat;
            lon = parsedLon;
        }
        else
        {
            try
            {
                var locations = await Geocoding.Default.GetLocationsAsync(ClinicAddress.Trim());
                var loc = locations?.FirstOrDefault();
                if (loc != null) { lat = loc.Latitude; lon = loc.Longitude; }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageClinics] geocode failed, falling back to defaults: {ex.Message}");
            }
        }

        string clinicId = string.Empty;
        string clinicName = ClinicName.Trim();
        var schedule = GetWeeklyScheduleFromUi();

        if (IsEditMode && _editingClinic != null)
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(
                Shell.Current.CurrentPage.Navigation, "Confirm Update", $"Update '{ClinicName}'?", "Yes", "No");
            if (!confirm) return;

            _editingClinic.Name = ClinicName.Trim();
            _editingClinic.Address = ClinicAddress.Trim();
            _editingClinic.Latitude = lat;
            _editingClinic.Longitude = lon;
            _editingClinic.UpdateSchedule(schedule);

            try
            {
                if (!string.IsNullOrEmpty(_editingClinic.FirestoreId))
                    await ClinicRepository.Instance.UpdateAsync(_editingClinic);
                clinicId = _editingClinic.FirestoreId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageClinics] update failed: {ex.Message}");
            }
        }
        else
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(
                Shell.Current.CurrentPage.Navigation, "Confirm Add", $"Add clinic '{ClinicName}'?", "Yes", "No");
            if (!confirm) return;

            var clinic = new Clinic
            {
                Name = ClinicName.Trim(),
                Address = ClinicAddress.Trim(),
                Latitude = lat,
                Longitude = lon
            };
            clinic.UpdateSchedule(schedule);

            try
            {
                await ClinicRepository.Instance.CreateAsync(clinic);
                clinicId = clinic.FirestoreId;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageClinics] create failed: {ex.Message}");
                clinic.Id = DatabaseService.StaticClinics.Count + 1;
                DatabaseService.StaticClinics.Add(clinic);
                clinicId = clinic.Id.ToString();
            }
        }

        // Update associated doctor (One-to-One)
        foreach (var doc in AvailableDoctors)
        {
            if (SelectedDoctor != null && !string.IsNullOrEmpty(SelectedDoctor.FirestoreId) && doc.FirestoreId == SelectedDoctor.FirestoreId)
            {
                doc.ClinicFirestoreId = clinicId;
                doc.ClinicName = clinicName;
                doc.UpdateSchedule(schedule);
                doc.Availability = $"{clinicName}: Open Hours";
                try
                {
                    if (!string.IsNullOrEmpty(doc.FirestoreId))
                        await DoctorRepository.Instance.UpdateAsync(doc);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ManageClinics] Failed to update doctor {doc.Name}: {ex.Message}");
                }
            }
            else if (!string.IsNullOrEmpty(clinicId) && doc.ClinicFirestoreId == clinicId)
            {
                doc.ClinicFirestoreId = string.Empty;
                doc.ClinicName = string.Empty;
                doc.Availability = string.Empty;
                doc.UpdateSchedule(new WeeklySchedule());
                try
                {
                    if (!string.IsNullOrEmpty(doc.FirestoreId))
                        await DoctorRepository.Instance.UpdateAsync(doc);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ManageClinics] Failed to disassociate doctor {doc.Name}: {ex.Message}");
                }
            }
        }

        CancelEditCommand.Execute(null);
        await LoadClinicsAsync();
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Done",
            IsEditMode ? "Clinic updated." : "Clinic saved successfully!");
    }

    [RelayCommand]
    async Task DeleteClinicAsync(Clinic clinic)
    {
        if (clinic == null) return;

        bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(
            Shell.Current.CurrentPage.Navigation, "Confirm Delete", $"Remove '{clinic.Name}'?", "Yes", "No");
        if (!confirm) return;

        try
        {
            if (!string.IsNullOrEmpty(clinic.FirestoreId))
                await ClinicRepository.Instance.DeleteAsync(clinic.FirestoreId);
            else
                DatabaseService.StaticClinics.Remove(clinic);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ManageClinics] delete failed: {ex.Message}");
            DatabaseService.StaticClinics.Remove(clinic);
        }

        if (_editingClinic?.FirestoreId == clinic.FirestoreId)
            CancelEditCommand.Execute(null);

        await LoadClinicsAsync();
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation,
            "Removed", $"'{clinic.Name}' has been removed.");
    }

    [RelayCommand]
    async Task BackAsync() => await Shell.Current.GoToAsync("//admindashboard");
}
