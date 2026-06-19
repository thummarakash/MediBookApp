using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

// Reusable checkbox item for any multi-select list
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
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private string _days = "Mon-Fri";
    public string Days
    {
        get => _days;
        set => SetProperty(ref _days, value);
    }

    private string _timeSlot = "09:00 AM - 05:00 PM";
    public string TimeSlot
    {
        get => _timeSlot;
        set => SetProperty(ref _timeSlot, value);
    }
}

public partial class ManageDoctorsViewModel : ObservableObject
{
    // Editing state
    private Doctor? _editingDoctor;

    [ObservableProperty]
    private string _formTitle = "Add New Doctor";

    [ObservableProperty]
    private string _saveButtonText = "Save Doctor";

    [ObservableProperty]
    private bool _isEditMode;

    // Basic fields
    [ObservableProperty]
    private string _doctorName = string.Empty;

    [ObservableProperty]
    private string _feePerSession = "90.00";

    // Multi-select dropdowns
    [ObservableProperty]
    private ObservableCollection<SelectionItem> _specialtySelections = new();

    [ObservableProperty]
    private ObservableCollection<SelectionItem> _departmentSelections = new();

    [ObservableProperty]
    private ObservableCollection<ClinicSelectionItem> _clinicSelections = new();

    [ObservableProperty]
    private ObservableCollection<Doctor> _doctors = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isClinicsDropdownExpanded;

    [ObservableProperty]
    private bool _isSpecialtiesExpanded;

    [ObservableProperty]
    private bool _isDepartmentsExpanded;

    public string SelectedSpecialtiesText =>
        SpecialtySelections.Any(s => s.IsSelected)
            ? string.Join(", ", SpecialtySelections.Where(s => s.IsSelected).Select(s => s.Name))
            : "Select Specialties";

    public string SelectedDepartmentsText =>
        DepartmentSelections.Any(d => d.IsSelected)
            ? string.Join(", ", DepartmentSelections.Where(d => d.IsSelected).Select(d => d.Name))
            : "Select Departments";

    public ManageDoctorsViewModel()
    {
        InitSpecialtiesAndDepartments();
    }

    private void InitSpecialtiesAndDepartments()
    {
        var specs = new[]
        {
            "General Practitioner", "Cardiologist", "Dermatologist",
            "Physiotherapist", "Neurologist", "Psychiatrist",
            "Paediatrician", "Gynaecologist", "Orthopaedic Surgeon", "Oncologist"
        };
        SpecialtySelections = new ObservableCollection<SelectionItem>(
            specs.Select(s => new SelectionItem { Name = s })
        );

        var depts = new[]
        {
            "General Care", "Heart Clinic", "Skin Care", "Physiotherapy",
            "Neurology", "Mental Health", "Women's Health", "Paediatrics",
            "Oncology", "Orthopaedics"
        };
        DepartmentSelections = new ObservableCollection<SelectionItem>(
            depts.Select(d => new SelectionItem { Name = d })
        );

        // Wire up property changed so the summary text updates
        foreach (var s in SpecialtySelections)
            s.PropertyChanged += (_, _) => OnPropertyChanged(nameof(SelectedSpecialtiesText));
        foreach (var d in DepartmentSelections)
            d.PropertyChanged += (_, _) => OnPropertyChanged(nameof(SelectedDepartmentsText));
    }

    [RelayCommand]
    private void ToggleSpecialtiesDropdown() => IsSpecialtiesExpanded = !IsSpecialtiesExpanded;

    [RelayCommand]
    private void ToggleDepartmentsDropdown() => IsDepartmentsExpanded = !IsDepartmentsExpanded;

    [RelayCommand]
    private void ToggleClinicsDropdown() => IsClinicsDropdownExpanded = !IsClinicsDropdownExpanded;

    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        try
        {
            var clinicList = await DatabaseService.Instance.GetClinicsAsync();
            var existingSelections = ClinicSelections.ToDictionary(c => c.Clinic.Id, c => c);

            var updatedSelections = clinicList.Select(c =>
            {
                if (existingSelections.TryGetValue(c.Id, out var existing))
                    return existing;
                return new ClinicSelectionItem { Clinic = c };
            });
            ClinicSelections = new ObservableCollection<ClinicSelectionItem>(updatedSelections);

            var doctorList = await DatabaseService.Instance.GetDoctorsAsync();
            foreach (var doc in doctorList)
            {
                var mappings = DatabaseService.StaticClinicDoctors.Where(cd => cd.DoctorId == doc.Id).ToList();
                doc.ClinicName = mappings.Any()
                    ? string.Join(", ", mappings
                        .Select(m => DatabaseService.StaticClinics.FirstOrDefault(c => c.Id == m.ClinicId)?.Name)
                        .Where(n => n != null))
                    : "No Clinic";
            }
            Doctors = new ObservableCollection<Doctor>(doctorList);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void EditDoctor(Doctor doctor)
    {
        if (doctor == null) return;

        _editingDoctor = doctor;
        IsEditMode = true;
        FormTitle = "Edit Doctor";
        SaveButtonText = "Update Doctor";

        DoctorName = doctor.Name;
        FeePerSession = doctor.FeePerAppointment.ToString("F2");

        // Restore specialty selections
        var savedSpecialties = doctor.Specialty?.Split(',').Select(s => s.Trim()).ToHashSet() ?? new HashSet<string>();
        foreach (var s in SpecialtySelections)
            s.IsSelected = savedSpecialties.Contains(s.Name);

        var savedDepts = doctor.Department?.Split(',').Select(d => d.Trim()).ToHashSet() ?? new HashSet<string>();
        foreach (var d in DepartmentSelections)
            d.IsSelected = savedDepts.Contains(d.Name);

        // Restore clinic selections
        var mappings = DatabaseService.StaticClinicDoctors.Where(cd => cd.DoctorId == doctor.Id).ToList();
        foreach (var cs in ClinicSelections)
            cs.IsSelected = mappings.Any(m => m.ClinicId == cs.Clinic.Id);

        IsSpecialtiesExpanded = false;
        IsDepartmentsExpanded = false;
        IsClinicsDropdownExpanded = false;
    }

    [RelayCommand]
    private void CancelEdit()
    {
        _editingDoctor = null;
        IsEditMode = false;
        FormTitle = "Add New Doctor";
        SaveButtonText = "Save Doctor";
        ClearForm();
    }

    [RelayCommand]
    private async Task SaveDoctorAsync()
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

        if (IsEditMode && _editingDoctor != null)
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(Shell.Current.CurrentPage.Navigation, "Confirm Update", $"Update details for '{DoctorName}'?", "Yes", "No");
            if (!confirm) return;

            _editingDoctor.Name = DoctorName.Trim();
            _editingDoctor.Specialty = specialtyStr;
            _editingDoctor.Department = deptStr;
            _editingDoctor.Availability = availability;
            _editingDoctor.FeePerAppointment = sessionFee;
            _editingDoctor.FeePerMinute = sessionFee / 30.0;

            // Update clinic mappings
            var oldMappings = DatabaseService.StaticClinicDoctors.Where(cd => cd.DoctorId == _editingDoctor.Id).ToList();
            foreach (var m in oldMappings)
                DatabaseService.StaticClinicDoctors.Remove(m);

            foreach (var sc in selectedClinics)
            {
                DatabaseService.StaticClinicDoctors.Add(new ClinicDoctor
                {
                    Id = DatabaseService.StaticClinicDoctors.Count + 1,
                    ClinicId = sc.Clinic.Id,
                    DoctorId = _editingDoctor.Id
                });
            }

            // Reset edit mode state properly
            _editingDoctor = null;
            IsEditMode = false;
            FormTitle = "Add New Doctor";
            SaveButtonText = "Save Doctor";
        }
        else
        {
            bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(Shell.Current.CurrentPage.Navigation, "Confirm Add", $"Register '{DoctorName}' as a doctor?", "Yes", "No");
            if (!confirm) return;

            var doctor = new Doctor
            {
                Id = DatabaseService.StaticDoctors.Count + 1,
                Name = DoctorName.Trim(),
                Specialty = specialtyStr,
                Department = deptStr,
                Availability = availability,
                FeePerAppointment = sessionFee,
                FeePerMinute = sessionFee / 30.0,
                SlotDurationMinutes = 30,
                Experience = "5 years",
                Rating = "5.0",
                Bio = "Professional healthcare provider."
            };

            DatabaseService.StaticDoctors.Add(doctor);

            foreach (var sc in selectedClinics)
            {
                DatabaseService.StaticClinicDoctors.Add(new ClinicDoctor
                {
                    Id = DatabaseService.StaticClinicDoctors.Count + 1,
                    ClinicId = sc.Clinic.Id,
                    DoctorId = doctor.Id
                });
            }
        }

        string msg = IsEditMode ? "Doctor updated." : "Doctor saved successfully!";
        ClearForm();
        await LoadDataAsync();
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Success", msg);
    }

    [RelayCommand]
    private async Task DeleteDoctorAsync(Doctor doctor)
    {
        if (doctor == null) return;

        bool confirm = await Pages.ConfirmationPopupPage.ShowConfirmAsync(Shell.Current.CurrentPage.Navigation, "Confirm Delete", $"Remove '{doctor.Name}' from the system?", "Yes", "No");
        if (!confirm) return;

        DatabaseService.StaticDoctors.Remove(doctor);

        var mappings = DatabaseService.StaticClinicDoctors.Where(cd => cd.DoctorId == doctor.Id).ToList();
        foreach (var m in mappings)
            DatabaseService.StaticClinicDoctors.Remove(m);

        if (_editingDoctor?.Id == doctor.Id)
            CancelEditCommand.Execute(null);

        await LoadDataAsync();
        await Pages.ConfirmationPopupPage.ShowAsync(Shell.Current.CurrentPage.Navigation, "Removed", $"'{doctor.Name}' has been removed.");
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("//admindashboard");
    }

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
