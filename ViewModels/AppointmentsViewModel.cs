using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class AppointmentsViewModel : ObservableObject
{
    private List<Appointment> _all = new();

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
            FilterByTab(SelectedTab);
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
        FilterByTab(tab);
    }

    private void FilterByTab(string tab)
    {
        var docs = _all.AsEnumerable();
        if (tab == "Past")
        {
            var filtered = docs
                .Where(a => a.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || 
                            a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(a => a.FullDateTime)
                .ToList();
            Appointments = new ObservableCollection<Appointment>(filtered);
            IsEmpty = filtered.Count == 0;
        }
        else
        {
            var filtered = docs
                .Where(a => a.Status.Equals(tab, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.FullDateTime)
                .ToList();
            Appointments = new ObservableCollection<Appointment>(filtered);
            IsEmpty = filtered.Count == 0;
        }
    }
}
