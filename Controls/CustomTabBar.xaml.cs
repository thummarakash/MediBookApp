using System;
using Microsoft.Maui.Controls;

namespace MediBook.Controls;

public partial class CustomTabBar : ContentView
{
    public static readonly BindableProperty ActiveTabProperty =
        BindableProperty.Create(nameof(ActiveTab), typeof(string), typeof(CustomTabBar), string.Empty, propertyChanged: OnActiveTabChanged);

    public string ActiveTab
    {
        get => (string)GetValue(ActiveTabProperty);
        set => SetValue(ActiveTabProperty, value);
    }

    public static readonly BindableProperty IsAdminProperty =
        BindableProperty.Create(nameof(IsAdmin), typeof(bool), typeof(CustomTabBar), false, propertyChanged: OnIsAdminChanged);

    public bool IsAdmin
    {
        get => (bool)GetValue(IsAdminProperty);
        set => SetValue(IsAdminProperty, value);
    }

    public Color HomeColor => ActiveTab == "Home" ? Color.FromArgb("#155EEF") : Color.FromArgb("#8A9EAD");
    public Color AppointmentsColor => ActiveTab == "Appointments" ? Color.FromArgb("#155EEF") : Color.FromArgb("#8A9EAD");
    public Color ClinicsColor => ActiveTab == "Clinics" ? Color.FromArgb("#155EEF") : Color.FromArgb("#8A9EAD");
    public Color DoctorsColor => ActiveTab == "Doctors" ? Color.FromArgb("#155EEF") : Color.FromArgb("#8A9EAD");
    public Color DocumentsColor => ActiveTab == "Documents" ? Color.FromArgb("#155EEF") : Color.FromArgb("#8A9EAD");
    public Color ProfileColor => ActiveTab == "Profile" ? Color.FromArgb("#155EEF") : Color.FromArgb("#8A9EAD");

    public string HomeIcon => ActiveTab == "Home" ? "tab_home_selected.svg" : "tab_home_unselected.svg";
    public string AppointmentsIcon => ActiveTab == "Appointments" ? "tab_appointments_selected.svg" : "tab_appointments_unselected.svg";
    public string ClinicsIcon => ActiveTab == "Clinics" ? "tab_clinics_selected.svg" : "tab_clinics_unselected.svg";
    public string DoctorsIcon => ActiveTab == "Doctors" ? "tab_doctors_selected.svg" : "tab_doctors_unselected.svg";
    public string DocumentsIcon => ActiveTab == "Documents" ? "tab_documents_selected.svg" : "tab_documents_unselected.svg";
    public string ProfileIcon => ActiveTab == "Profile" ? "tab_profile_selected.svg" : "tab_profile_unselected.svg";

    public CustomTabBar()
    {
        InitializeComponent();
        UpdateGridVisibilities();
    }

    private static void OnActiveTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not CustomTabBar bar) return;

        bar.OnPropertyChanged(nameof(HomeColor));
        bar.OnPropertyChanged(nameof(AppointmentsColor));
        bar.OnPropertyChanged(nameof(ClinicsColor));
        bar.OnPropertyChanged(nameof(DoctorsColor));
        bar.OnPropertyChanged(nameof(DocumentsColor));
        bar.OnPropertyChanged(nameof(ProfileColor));

        bar.OnPropertyChanged(nameof(HomeIcon));
        bar.OnPropertyChanged(nameof(AppointmentsIcon));
        bar.OnPropertyChanged(nameof(ClinicsIcon));
        bar.OnPropertyChanged(nameof(DoctorsIcon));
        bar.OnPropertyChanged(nameof(DocumentsIcon));
        bar.OnPropertyChanged(nameof(ProfileIcon));
    }

    private static void OnIsAdminChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CustomTabBar bar)
            bar.UpdateGridVisibilities();
    }

    private void UpdateGridVisibilities()
    {
        if (PatientGrid != null && AdminGrid != null)
        {
            PatientGrid.IsVisible = !IsAdmin;
            AdminGrid.IsVisible = IsAdmin;
        }
    }

    private async void OnHomeTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Home")
            await Shell.Current.GoToAsync("//home");
    }

    private async void OnAppointmentsTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Appointments")
            await Shell.Current.GoToAsync("//appointments");
    }

    private async void OnClinicsTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Clinics")
            await Shell.Current.GoToAsync("//clinics");
    }

    private async void OnDoctorsTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Doctors")
            await Shell.Current.GoToAsync("//doctors");
    }

    private async void OnDocumentsTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Documents")
            await Shell.Current.GoToAsync("//documents");
    }

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Profile")
            await Shell.Current.GoToAsync("//profile");
    }

    private async void OnAdminDashboardTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Home")
            await Shell.Current.GoToAsync("//admindashboard");
    }

    private async void OnAdminClinicsTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Clinics")
            await Shell.Current.GoToAsync("//adminclinics");
    }

    private async void OnAdminDoctorsTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Doctors")
            await Shell.Current.GoToAsync("//admindoctors");
    }

    private async void OnAdminAppointmentsTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Appointments")
            await Shell.Current.GoToAsync("//adminappointments");
    }

    private async void OnAdminProfileTapped(object sender, EventArgs e)
    {
        if (ActiveTab != "Profile")
            await Shell.Current.GoToAsync("//adminprofile");
    }
}
