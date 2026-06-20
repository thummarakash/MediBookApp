using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class AppointmentsViewModel : ObservableObject
{
    private List<Appointment> _all = new();

    [ObservableProperty] string searchQuery = string.Empty;
    [ObservableProperty] ObservableCollection<Appointment> appointments = new();
    [ObservableProperty] string selectedTab = "Upcoming";
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isEmpty;

    [RelayCommand]
    async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            _all = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            FilterAndSearch();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void SelectTab(string tab)
    {
        SelectedTab = tab;
        FilterAndSearch();
    }

    [RelayCommand]
    void Search(string query)
    {
        SearchQuery = query ?? string.Empty;
        FilterAndSearch();
    }

    private void FilterAndSearch()
    {
        var docs = _all.AsEnumerable();
        
        // Tab Filter
        if (SelectedTab == "Past")
        {
            docs = docs.Where(a => a.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || 
                                   a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            docs = docs.Where(a => a.Status.Equals(SelectedTab, StringComparison.OrdinalIgnoreCase));
        }

        // Search Filter
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            docs = docs.Where(a => a.DoctorName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                                || a.ClinicName.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase)
                                || a.Department.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase));
        }

        var sorted = SelectedTab == "Past"
            ? docs.OrderByDescending(a => a.FullDateTime).ToList()
            : docs.OrderBy(a => a.FullDateTime).ToList();

        Appointments = new ObservableCollection<Appointment>(sorted);
        IsEmpty = sorted.Count == 0;
    }
}
