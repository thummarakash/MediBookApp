using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Repositories;
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

    [ObservableProperty] string formTitle = "Add New Clinic";
    [ObservableProperty] string saveButtonText = "Save Clinic";
    [ObservableProperty] bool isEditMode;
    [ObservableProperty] string clinicName = string.Empty;
    [ObservableProperty] string clinicAddress = string.Empty;
    [ObservableProperty] ObservableCollection<Clinic> clinics = new();
    [ObservableProperty] ObservableCollection<DoctorSelectionItem> doctorSelections = new();
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isDoctorsDropdownExpanded;

    [RelayCommand]
    void ToggleDoctorsDropdown() => IsDoctorsDropdownExpanded = !IsDoctorsDropdownExpanded;

    [RelayCommand]
    async Task LoadClinicsAsync()
    {
        IsLoading = true;
        try
        {
            var list = await DatabaseService.Instance.GetClinicsAsync();
            Clinics = new ObservableCollection<Clinic>(list);

            var doctors = await DatabaseService.Instance.GetDoctorsAsync();
            var existing = DoctorSelections.ToDictionary(ds => ds.Doctor.Name, ds => ds.IsSelected);
            DoctorSelections = new ObservableCollection<DoctorSelectionItem>(
                doctors.Select(d => new DoctorSelectionItem
                {
                    Doctor = d,
                    IsSelected = existing.TryGetValue(d.Name, out var sel) && sel
                })
            );
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
        FormTitle = "Edit Clinic";
        SaveButtonText = "Update Clinic";
        ClinicName = clinic.Name;
        ClinicAddress = clinic.Address;
        foreach (var ds in DoctorSelections) ds.IsSelected = false;
        IsDoctorsDropdownExpanded = false;
    }

    [RelayCommand]
    void CancelEdit()
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
    async Task SaveClinicAsync()
    {
        if (string.IsNullOrWhiteSpace(ClinicName) || string.IsNullOrWhiteSpace(ClinicAddress))
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation,
                "Missing Details", "Please enter both name and address.", "icon_warning.svg");
            return;
        }

        double lat = -37.8136, lon = 144.9631;
        try
        {
            var locations = await Geocoding.Default.GetLocationsAsync(ClinicAddress.Trim());
            var loc = locations?.FirstOrDefault();
            if (loc != null) { lat = loc.Latitude; lon = loc.Longitude; }
        }
        catch { /* use defaults */ }

        if (IsEditMode && _editingClinic != null)
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(
                Shell.Current.CurrentPage.Navigation, "Confirm Update", $"Update '{ClinicName}'?", "Yes", "No");
            if (!confirm) return;

            _editingClinic.Name = ClinicName.Trim();
            _editingClinic.Address = ClinicAddress.Trim();
            _editingClinic.Latitude = lat;
            _editingClinic.Longitude = lon;

            try
            {
                if (!string.IsNullOrEmpty(_editingClinic.FirestoreId))
                    await ClinicRepository.Instance.UpdateAsync(_editingClinic);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firestore] Clinic update failed: {ex.Message}");
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

            try
            {
                await ClinicRepository.Instance.CreateAsync(clinic);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Firestore] Clinic create failed: {ex.Message}");
                // Still add to static list as fallback
                clinic.Id = DatabaseService.StaticClinics.Count + 1;
                DatabaseService.StaticClinics.Add(clinic);
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
            System.Diagnostics.Debug.WriteLine($"[Firestore] Clinic delete failed: {ex.Message}");
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
