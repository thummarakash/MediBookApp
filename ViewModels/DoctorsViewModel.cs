using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediBook.Models;
using MediBook.Services;

namespace MediBook.ViewModels;

public partial class DoctorsViewModel : ObservableObject
{
    private List<Doctor> _drList = new();

    [ObservableProperty] ObservableCollection<Doctor> doctors = new();
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isEmpty;
    [ObservableProperty] string selectedCategory = "All";
    [ObservableProperty] string searchText = "";

    [RelayCommand]
    async Task RefreshList()
    {
        IsLoading = true;
        try
        {
            var res = await DatabaseService.Instance.GetDoctorsAsync();
            _drList = res ?? new List<Doctor>();
            CompileDoctors();
        }
        catch (Exception cli_ex)
        {
            System.Diagnostics.Debug.WriteLine("ERR_DOC_LOAD: " + cli_ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    void ChooseCategory(string cat)
    {
        SelectedCategory = cat ?? "All";
        CompileDoctors();
    }

    [RelayCommand]
    void FilterDoctorsByText(string val)
    {
        SearchText = val?.Trim() ?? "";
        CompileDoctors();
    }

    private void CompileDoctors()
    {
        var temp = new List<Doctor>();
        
        for (int i = 0; i < _drList.Count; i++)
        {
            var item = _drList[i];
            
            bool catMatch = false;
            if (SelectedCategory == "All")
            {
                catMatch = true;
            }
            else
            {
                catMatch = (item.Specialty != null && item.Specialty.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase)) ||
                           (item.Department != null && item.Department.Contains(SelectedCategory, StringComparison.OrdinalIgnoreCase));
            }

            if (!catMatch) continue;

            if (!string.IsNullOrEmpty(SearchText))
            {
                var q = SearchText.ToLowerInvariant();
                bool textMatch = (item.Name != null && item.Name.ToLower().Contains(q)) ||
                                 (item.Department != null && item.Department.ToLower().Contains(q)) ||
                                 (item.Specialty != null && item.Specialty.ToLower().Contains(q));
                
                if (!textMatch) continue;
            }

            temp.Add(item);
        }

        Doctors = new ObservableCollection<Doctor>(temp);
        IsEmpty = temp.Count == 0;
    }
}
