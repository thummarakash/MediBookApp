using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class DoctorsViewModel : ObservableObject
{
    private List<Doctor> _allDoctors = new();

    [ObservableProperty] ObservableCollection<Doctor> doctors = new();
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isEmpty;
    [ObservableProperty] string searchText = string.Empty;

    [RelayCommand]
    async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allDoctors = await DatabaseService.Instance.GetDoctorsAsync();
            Doctors = new ObservableCollection<Doctor>(_allDoctors);
            IsEmpty = !_allDoctors.Any();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void Search(string text)
    {
        var query = text?.Trim().ToLowerInvariant() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(query))
        {
            Doctors = new ObservableCollection<Doctor>(_allDoctors);
        }
        else
        {
            var filtered = _allDoctors.Where(d =>
                d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Department.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Specialty.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
            Doctors = new ObservableCollection<Doctor>(filtered);
        }
        IsEmpty = Doctors.Count == 0;
    }
}
