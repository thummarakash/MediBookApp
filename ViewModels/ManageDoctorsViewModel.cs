using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Repositories;
using MediBook.Services;

namespace MediBook.ViewModels;

public class SelectionItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public class ClinicSelectionItem : ObservableObject
{
    public Clinic Clinic { get; set; } = null!;

    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }

    private string _days = "Mon-Fri";
    public string Days { get => _days; set => SetProperty(ref _days, value); }

    private string _timeSlot = "09:00 AM - 05:00 PM";
    public string TimeSlot { get => _timeSlot; set => SetProperty(ref _timeSlot, value); }
}

public partial class ManageDoctorsViewModel : ObservableObject
{
    private Doctor? _editingDoctor;

    [ObservableProperty] string formTitle = "Add New Doctor";
    [ObservableProperty] string saveButtonText = "Save Doctor";
    [ObservableProperty] bool isEditMode;
    [ObservableProperty] string doctorName = string.Empty;
    [ObservableProperty] string feePerSession = "90.00";
    [ObservableProperty] ObservableCollection<SelectionItem> specialtySelections = new();
    [ObservableProperty] ObservableCollection<SelectionItem> departmentSelections = new();
    [ObservableProperty] ObservableCollection<ClinicSelectionItem> clinicSelections = new();
    [ObservableProperty] ObservableCollection<Doctor> doctors = new();
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isClinicsDropdownExpanded;
    [ObservableProperty] bool isSpecialtiesExpanded;
    [ObservableProperty] bool isDepartmentsExpanded;

    public string SelectedSpecialtiesText =>
        SpecialtySelections.Any(s => s.IsSelected)
            ? string.Join(", ", SpecialtySelections.Where(s => s.IsSelected).Select(s => s.Name))
            : "Select Specialties";

    public string SelectedDepartmentsText =>
        DepartmentSelections.Any(d => d.IsSelected)
            ? string.Join(", ", DepartmentSelections.Where(d => d.IsSelected).Select(d => d.Name))
            : "Select Departments";

    public ManageDoctorsViewModel() => InitSpecialtiesAndDepartments();

    private void InitSpecialtiesAndDepartments()
    {
        var specs = new[] { "General Practitioner", "Cardiologist", "Dermatologist", "Physiotherapist", "Neurologist", "Psychiatrist", "Paediatrician", "Gynaecologist", "Orthopaedic Surgeon", "Oncologist" };
        SpecialtySelections = new ObservableCollection<SelectionItem>(specs.Select(s => new SelectionItem { Name = s }));

        var depts = new[] { "General Care", "Heart Clinic", "Skin Care", "Physiotherapy", "Neurology", "Mental Health", "Women's Health", "Paediatrics", "Oncology", "Orthopaedics" };
        DepartmentSelections = new ObservableCollection<SelectionItem>(depts.Select(d => new SelectionItem { Name = d }));

        foreach (var s in SpecialtySelections)
            s.PropertyChanged += (_, _) => OnPropertyChanged(nameof(SelectedSpecialtiesText));
        foreach (var d in DepartmentSelections)
            d.PropertyChanged += (_, _) => OnPropertyChanged(nameof(SelectedDepartmentsText));
    }

    [RelayCommand] void ToggleSpecialtiesDropdown() => IsSpecialtiesExpanded = !IsSpecialtiesExpanded;
    [RelayCommand] void ToggleDepartmentsDropdown() => IsDepartmentsExpanded = !IsDepartmentsExpanded;
    [RelayCommand] void ToggleClinicsDropdown() => IsClinicsDropdownExpanded = !IsClinicsDropdownExpanded;

    [RelayCommand]
    async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var clinicList = await DatabaseService.Instance.GetClinicsAsync();
            var existingSelections = ClinicSelections.ToDictionary(c => c.Clinic.Name, c => c);
            ClinicSelections = new ObservableCollection<ClinicSelectionItem>(
                clinicList.Select(c => existingSelections.TryGetValue(c.Name, out var ex)
                    ? ex
                    : new ClinicSelectionItem { Clinic = c }));

            var doctorList = await DatabaseService.Instance.GetDoctorsAsync();
            Doctors = new ObservableCollection<Doctor>(doctorList);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void EditDoctor(Doctor doctor)
    {
        if (doctor == null) return;
        _editingDoctor = doctor;
        IsEditMode = true;
        FormTitle = "Edit Doctor";
        SaveButtonText = "Update Doctor";
        DoctorName = doctor.Name;
        FeePerSession = doctor.FeePerAppointment.ToString("F2");

        var savedSpecialties = doctor.Specialty?.Split(',').Select(s => s.Trim()).ToHashSet() ?? new HashSet<string>();
        foreach (var s in SpecialtySelections) s.IsSelected = savedSpecialties.Contains(s.Name);

        var savedDepts = doctor.Department?.Split(',').Select(d => d.Trim()).ToHashSet() ?? new HashSet<string>();
        foreach (var d in DepartmentSelections) d.IsSelected = savedDepts.Contains(d.Name);

        IsSpecialtiesExpanded = false;
        IsDepartmentsExpanded = false;
        IsClinicsDropdownExpanded = false;
    }

    [RelayCommand]
    void CancelEdit()
    {
        _editingDoctor = null;
        IsEditMode = false;
        FormTitle = "Add New Doctor";
        SaveButtonText = "Save Doctor";
        ClearForm();
    }

    [RelayCommand]
    async Task SaveDoctorAsync()
    {
        if (string.IsNullOrWhiteSpace(DoctorName))
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Missing Details", "Please enter the doctor's name.", "icon_warning.svg");
            return;
        }

        var selectedSpecialties = SpecialtySelections.Where(s => s.IsSelected).ToList();
        if (!selectedSpecialties.Any())
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Required", "Please select at least one specialty.", "icon_warning.svg");
            return;
        }

        var selectedDepts = DepartmentSelections.Where(d => d.IsSelected).ToList();
        if (!selectedDepts.Any())
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Required", "Please select at least one department.", "icon_warning.svg");
            return;
        }

        var selectedClinics = ClinicSelections.Where(c => c.IsSelected).ToList();
        if (!selectedClinics.Any())
        {
            await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Required", "Please assign the doctor to at least one clinic.", "icon_warning.svg");
            return;
        }

        double.TryParse(FeePerSession, out var sessionFee);
        if (sessionFee <= 0) sessionFee = 90.00;

        string specialtyStr = string.Join(", ", selectedSpecialties.Select(s => s.Name));
        string deptStr = string.Join(", ", selectedDepts.Select(d => d.Name));
        string availability = string.Join(" | ", selectedClinics.Select(sc => $"{sc.Clinic.Name}: {sc.Days} • {sc.TimeSlot}"));
        string primaryClinicName = selectedClinics.First().Clinic.Name;
        string primaryClinicId = selectedClinics.First().Clinic.FirestoreId;

        if (IsEditMode && _editingDoctor != null)
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(
                Shell.Current.CurrentPage.Navigation, "Confirm Update", $"Update details for '{DoctorName}'?", "Yes", "No");
            if (!confirm) return;

            _editingDoctor.Name = DoctorName.Trim();
            _editingDoctor.Specialty = specialtyStr;
            _editingDoctor.Department = deptStr;
            _editingDoctor.Availability = availability;
            _editingDoctor.FeePerAppointment = sessionFee;
            _editingDoctor.FeePerMinute = sessionFee / 30.0;
            _editingDoctor.ClinicName = primaryClinicName;
            _editingDoctor.ClinicFirestoreId = primaryClinicId;

            try
            {
                if (!string.IsNullOrEmpty(_editingDoctor.FirestoreId))
                    await DoctorRepository.Instance.UpdateAsync(_editingDoctor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageDoctors] update error: {ex.Message}");
            }

            _editingDoctor = null;
            IsEditMode = false;
            FormTitle = "Add New Doctor";
            SaveButtonText = "Save Doctor";
        }
        else
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(
                Shell.Current.CurrentPage.Navigation, "Confirm Add", $"Register '{DoctorName}' as a doctor?", "Yes", "No");
            if (!confirm) return;

            var doctor = new Doctor
            {
                Name = DoctorName.Trim(),
                Specialty = specialtyStr,
                Department = deptStr,
                Availability = availability,
                FeePerAppointment = sessionFee,
                FeePerMinute = sessionFee / 30.0,
                SlotDurationMinutes = 30,
                Experience = "5 years",
                Rating = "5.0",
                Bio = "Professional healthcare provider.",
                ClinicName = primaryClinicName,
                ClinicFirestoreId = primaryClinicId
            };

            try
            {
                await DoctorRepository.Instance.CreateAsync(doctor);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ManageDoctors] Firestore create failed, added to static list: {ex.Message}");
                doctor.Id = DatabaseService.StaticDoctors.Count + 1;
                DatabaseService.StaticDoctors.Add(doctor);
            }
        }

        ClearForm();
        await LoadDataAsync();
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Success",
            IsEditMode ? "Doctor updated." : "Doctor saved successfully!");
    }

    [RelayCommand]
    async Task DeleteDoctorAsync(Doctor doctor)
    {
        if (doctor == null) return;

        bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(
            Shell.Current.CurrentPage.Navigation, "Confirm Delete", $"Remove '{doctor.Name}' from the system?", "Yes", "No");
        if (!confirm) return;

        try
        {
            if (!string.IsNullOrEmpty(doctor.FirestoreId))
                await DoctorRepository.Instance.DeleteAsync(doctor.FirestoreId);
            else
                DatabaseService.StaticDoctors.Remove(doctor);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ManageDoctors] delete failed: {ex.Message}");
            DatabaseService.StaticDoctors.Remove(doctor);
        }

        if (_editingDoctor?.FirestoreId == doctor.FirestoreId)
            CancelEditCommand.Execute(null);

        await LoadDataAsync();
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation,
            "Removed", $"'{doctor.Name}' has been removed.");
    }

    [RelayCommand]
    async Task BackAsync() => await Shell.Current.GoToAsync("//admindashboard");

    private void ClearForm()
    {
        DoctorName = string.Empty;
        FeePerSession = "90.00";
        foreach (var s in SpecialtySelections) s.IsSelected = false;
        foreach (var d in DepartmentSelections) d.IsSelected = false;
        foreach (var c in ClinicSelections) c.IsSelected = false;
        IsSpecialtiesExpanded = false;
        IsDepartmentsExpanded = false;
        IsClinicsDropdownExpanded = false;
    }
}
