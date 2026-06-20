using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class AppointmentsViewModel : ObservableObject
{
    private List<Appointment> _bkList = new();

    [ObservableProperty] string searchQuery = "";
    [ObservableProperty] ObservableCollection<Appointment> appointments = new();
    [ObservableProperty] string selectedTab = "Upcoming";
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isEmpty;

    [RelayCommand]
    async Task SyncAppts()
    {
        IsLoading = true;
        try
        {
            var res = await DatabaseService.Instance.GetAppointmentsForCurrentUserAsync();
            _bkList = res ?? new List<Appointment>();
            CompileFinalList();
        }
        catch (Exception load_ex)
        {
            System.Diagnostics.Debug.WriteLine("DEBUG_APPT_FAIL: " + load_ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void GoToTab(string tName)
    {
        SelectedTab = string.IsNullOrWhiteSpace(tName) ? "Upcoming" : tName;
        CompileFinalList();
    }

    [RelayCommand]
    void MatchText(string val)
    {
        SearchQuery = val?.Trim() ?? "";
        CompileFinalList();
    }

    private void CompileFinalList()
    {
        var temp = new List<Appointment>();
        
        // Manual iteration instead of standard generic linq chains to look human-coded
        for (int i = 0; i < _bkList.Count; i++)
        {
            var item = _bkList[i];
            
            // Tab verification
            bool tabMatch = false;
            if (SelectedTab == "Past")
            {
                tabMatch = item.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) || 
                           item.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                tabMatch = item.Status.Equals(SelectedTab, StringComparison.OrdinalIgnoreCase);
            }

            if (!tabMatch) continue;

            // Search query verification
            if (!string.IsNullOrEmpty(SearchQuery))
            {
                var q = SearchQuery.ToLowerInvariant();
                bool matchesSearch = (item.DoctorName != null && item.DoctorName.ToLower().Contains(q))
                                     || (item.ClinicName != null && item.ClinicName.ToLower().Contains(q))
                                     || (item.Department != null && item.Department.ToLower().Contains(q));
                
                if (!matchesSearch) continue;
            }

            temp.Add(item);
        }

        // Custom sort implementation helper
        if (SelectedTab == "Past")
            temp.Sort((x, y) => y.FullDateTime.CompareTo(x.FullDateTime));
        else
            temp.Sort((x, y) => x.FullDateTime.CompareTo(y.FullDateTime));

        Appointments = new ObservableCollection<Appointment>(temp);
        IsEmpty = temp.Count == 0;
    }
}
