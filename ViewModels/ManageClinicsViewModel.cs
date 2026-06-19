using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public class DoctorSelectionItem : ObservableObject
{
    public Doctor Doctor { get; set; } = null!;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public partial class ManageClinicsViewModel : ObservableObject
{
    private Clinic? _editingClinic;

    [ObservableProperty]
    private string _formTitle = "Add New Clinic";

    [ObservableProperty]
    private string _saveButtonText = "Save Clinic";

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private string _clinicName = string.Empty;

    [ObservableProperty]
    private string _clinicAddress = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Clinic> _clinics = new();

    [ObservableProperty]
    private ObservableCollection<DoctorSelectionItem> _doctorSelections = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isDoctorsDropdownExpanded;

    [RelayCommand]
    private void ToggleDoctorsDropdown() => IsDoctorsDropdownExpanded = !IsDoctorsDropdownExpanded;

    [RelayCommand]
    private async Task LoadClinicsAsync()
    {
        IsLoading = true;
        try
        {
            var list = await DatabaseService.Instance.GetClinicsAsync();
            Clinics = new ObservableCollection<Clinic>(list);

            var doctors = await DatabaseService.Instance.GetDoctorsAsync();
            var existing = DoctorSelections.ToDictionary(ds => ds.Doctor.Id, ds => ds.IsSelected);
            DoctorSelections = new ObservableCollection<DoctorSelectionItem>(
                doctors.Select(d => new DoctorSelectionItem
                {
                    Doctor = d,
                    IsSelected = existing.TryGetValue(d.Id, out var sel) && sel
                })
            );
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EditClinic(Clinic clinic)
    {
        if (clinic == null) return;

        _editingClinic = clinic;
        IsEditMode = true;
        FormTitle = "Edit Clinic";
        SaveButtonText = "Update Clinic";
        ClinicName = clinic.Name;
        ClinicAddress = clinic.Address;

        // Restore doctor selections for this clinic
        var mappedDoctorIds = DatabaseService.StaticClinicDoctors
            .Where(cd => cd.ClinicId == clinic.Id)
            .Select(cd => cd.DoctorId)
            .ToHashSet();

        foreach (var ds in DoctorSelections)
            ds.IsSelected = mappedDoctorIds.Contains(ds.Doctor.Id);

        IsDoctorsDropdownExpanded = false;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingClinic = null;
        IsEditMode = false;
        FormTitle = "Add New Clinic";
        SaveButtonText = "Save Clinic";
        ClinicName = string.Empty;
        ClinicAddress = string.Empty;
        foreach (var ds in DoctorSelections) ds.IsSelected = false;
        IsDoctorsDropdownExpanded = false;
    }

    [RelayCommand]
    private async Task SaveClinicAsync()
    {
        if (string.IsNullOrWhiteSpace(ClinicName) || string.IsNullOrWhiteSpace(ClinicAddress))
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Missing Details", "Please enter both name and address.", "icon_warning.svg");
            return;
        }

        double lat = -37.8136;
        double lon = 144.9631;

        try
        {
            var locations = await Geocoding.Default.GetLocationsAsync(ClinicAddress.Trim());
            var location = locations?.FirstOrDefault();
            if (location != null)
            {
                lat = location.Latitude;
                lon = location.Longitude;
            }
        }
        catch
        {
            lat = -37.8136 + (DatabaseService.StaticClinics.Count * 0.005);
            lon = 144.9631 + (DatabaseService.StaticClinics.Count * 0.005);
        }

        var selectedDocs = DoctorSelections.Where(ds => ds.IsSelected).ToList();

        if (IsEditMode && _editingClinic != null)
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(Shell.Current.CurrentPage.Navigation, "Confirm Update", $"Update '{ClinicName}'?", "Yes", "No");
            if (!confirm) return;

            _editingClinic.Name = ClinicName.Trim();
            _editingClinic.Address = ClinicAddress.Trim();
            _editingClinic.Latitude = lat;
            _editingClinic.Longitude = lon;

            // Refresh doctor mappings
            var oldMappings = DatabaseService.StaticClinicDoctors.Where(cd => cd.ClinicId == _editingClinic.Id).ToList();
            foreach (var m in oldMappings)
                DatabaseService.StaticClinicDoctors.Remove(m);

            foreach (var sd in selectedDocs)
            {
                DatabaseService.StaticClinicDoctors.Add(new ClinicDoctor
                {
                    Id = DatabaseService.StaticClinicDoctors.Count + 1,
                    ClinicId = _editingClinic.Id,
                    DoctorId = sd.Doctor.Id
                });
            }

            CancelEditCommand.Execute(null);
        }
        else
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(Shell.Current.CurrentPage.Navigation, "Confirm Add", $"Add clinic '{ClinicName}'?", "Yes", "No");
            if (!confirm) return;

            var clinic = new Clinic
            {
                Id = DatabaseService.StaticClinics.Count + 1,
                Name = ClinicName.Trim(),
                Address = ClinicAddress.Trim(),
                Latitude = lat,
                Longitude = lon
            };

            DatabaseService.StaticClinics.Add(clinic);

            foreach (var sd in selectedDocs)
            {
                DatabaseService.StaticClinicDoctors.Add(new ClinicDoctor
                {
                    Id = DatabaseService.StaticClinicDoctors.Count + 1,
                    ClinicId = clinic.Id,
                    DoctorId = sd.Doctor.Id
                });
            }

            ClinicName = string.Empty;
            ClinicAddress = string.Empty;
            IsDoctorsDropdownExpanded = false;
            foreach (var ds in DoctorSelections) ds.IsSelected = false;
        }

        await LoadClinicsAsync();
        
        string msg = IsEditMode ? "Clinic updated." : "Clinic saved successfully!";
        // Reset edit mode state properly
        _editingClinic = null;
        IsEditMode = false;
        FormTitle = "Add New Clinic";
        SaveButtonText = "Save Clinic";
        ClinicName = string.Empty;
        ClinicAddress = string.Empty;
        foreach (var ds in DoctorSelections) ds.IsSelected = false;
        IsDoctorsDropdownExpanded = false;
        
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Done", msg);
    }

    [RelayCommand]
    private async Task DeleteClinicAsync(Clinic clinic)
    {
        if (clinic == null) return;

        bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(Shell.Current.CurrentPage.Navigation, "Confirm Delete", $"Remove '{clinic.Name}'?", "Yes", "No");
        if (!confirm) return;

        DatabaseService.StaticClinics.Remove(clinic);

        var mappings = DatabaseService.StaticClinicDoctors.Where(cd => cd.ClinicId == clinic.Id).ToList();
        foreach (var m in mappings)
            DatabaseService.StaticClinicDoctors.Remove(m);

        if (_editingClinic?.Id == clinic.Id)
            CancelEditCommand.Execute(null);

        await LoadClinicsAsync();
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Removed", $"'{clinic.Name}' has been removed.");
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("//admindashboard");
    }
}
