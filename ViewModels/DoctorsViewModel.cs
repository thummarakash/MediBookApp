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
    [ObservableProperty] string searchText = "";

    [RelayCommand]
    async Task LoadDoctorsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await DatabaseService.Instance.GetDoctorsAsync();
            _allDoctors = result ?? new List<Doctor>();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DoctorsVM] load error - {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void SelectSpecialtyCategory(string category)
    {
        SelectedCategory = category ?? "All";
        ApplyFilters();
    }

    [RelayCommand]
    void SearchDoctors(string text)
    {
        SearchText = text?.Trim() ?? "";
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = new List<Doctor>();

        for (int i = 0; i < _allDoctors.Count; i++)
        {
            var doc = _allDoctors[i];

            // match against both Specialty and Department — some doctors are listed under one or the other
            bool categoryMatch = SelectedCategory == "All"
                || (doc.Specialty != null && doc.Specialty.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                || (doc.Department != null && doc.Department.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase));

            if (!categoryMatch) continue;

            if (!string.IsNullOrEmpty(SearchText))
            {
                var q = SearchText.ToLowerInvariant();
                bool textMatch = (doc.Name != null && doc.Name.ToLower().Contains(q))
                              || (doc.Department != null && doc.Department.ToLower().Contains(q))
                              || (doc.Specialty != null && doc.Specialty.ToLower().Contains(q));

                if (!textMatch) continue;
            }

            filtered.Add(doc);
        }

        Doctors = new ObservableCollection<Doctor>(filtered);
        IsEmpty = filtered.Count == 0;
    }
}
