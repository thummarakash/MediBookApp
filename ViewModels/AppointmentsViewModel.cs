using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class AppointmentsViewModel : ObservableObject
{
    private List<Appointment> _allAppointments = new();

    [ObservableProperty] string searchQuery = "";
    [ObservableProperty] ObservableCollection<Appointment> appointments = new();
    [ObservableProperty] string selectedTab = "Upcoming";
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isEmpty;

    [RelayCommand]
    async Task LoadAppointmentsAsync()
    {
        IsLoading = true;
        try
        {
            var result = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            _allAppointments = result ?? new List<Appointment>();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApptsVM] LoadAppointmentsAsync: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void SelectTab(string tabName)
    {
        SelectedTab = string.IsNullOrWhiteSpace(tabName) ? "Upcoming" : tabName;
        ApplyFilters();
    }

    [RelayCommand]
    void SearchAppointments(string text)
    {
        SearchQuery = text?.Trim() ?? "";
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = new List<Appointment>();

        for (int i = 0; i < _allAppointments.Count; i++)
        {
            var item = _allAppointments[i];

            // Past tab shows both Completed and Cancelled
            bool tabMatch;
            if (SelectedTab == "Past")
            {
                tabMatch = item.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                        || item.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                tabMatch = item.Status.Equals(SelectedTab, StringComparison.OrdinalIgnoreCase);
            }

            if (!tabMatch) continue;

            if (!string.IsNullOrEmpty(SearchQuery))
            {
                var q = SearchQuery.ToLowerInvariant();
                bool hit = (item.DoctorName != null && item.DoctorName.ToLower().Contains(q))
                        || (item.ClinicName != null && item.ClinicName.ToLower().Contains(q))
                        || (item.Department != null && item.Department.ToLower().Contains(q));

                if (!hit) continue;
            }

            filtered.Add(item);
        }

        // past: newest at top so most recent visit shows first; upcoming: next appointment first
        if (SelectedTab == "Past")
            filtered.Sort((x, y) => y.FullDateTime.CompareTo(x.FullDateTime));
        else
            filtered.Sort((x, y) => x.FullDateTime.CompareTo(y.FullDateTime));

        Appointments = new ObservableCollection<Appointment>(filtered);
        IsEmpty = filtered.Count == 0;
    }
}
