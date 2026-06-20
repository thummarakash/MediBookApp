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
    [ObservableProperty] string selectedCategory = "All";
    [ObservableProperty] string searchText = string.Empty;

    [RelayCommand]
    async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _allDoctors = await DatabaseService.Instance.GetDoctorsAsync();
            ApplyFilters();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void SelectCategory(string category)
    {
        SelectedCategory = category;
        ApplyFilters();
    }

    [RelayCommand]
    void Search(string text)
    {
        SearchText = text ?? string.Empty;
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allDoctors.AsEnumerable();

        if (!SelectedCategory.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            filtered = filtered.Where(d => 
                d.Specialty.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase) ||
                d.Department.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase));
        }

        var query = SearchText.Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(query))
        {
            filtered = filtered.Where(d =>
                d.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Department.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                d.Specialty.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        Doctors = new ObservableCollection<Doctor>(filtered.ToList());
        IsEmpty = Doctors.Count == 0;
    }
}
