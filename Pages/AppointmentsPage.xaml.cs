using MediBook.Helpers;
using MediBook.ViewModels;

namespace MediBook.Pages;

public partial class AppointmentsPage : ContentPage
{
    private readonly AppointmentsViewModel _vm = new();
    private bool _firstAppear = true;

    public AppointmentsPage()
    {
        InitializeComponent();
        BindingContext = _vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_firstAppear)
        {
            _firstAppear = false;
            await AnimationHelper.PageEntranceAsync(this);
        }

        var user = await MediBook.Services.DatabaseService.Instance.GetCurrentUserAsync();
        if (user != null)
            CustomTabBarControl.IsAdmin = user.Role == "Admin";

        await _vm.LoadCommand.ExecuteAsync(null);
    }

    private void OnTabTapped(object sender, EventArgs e)
    {
        if (sender is Border border
            && border.GestureRecognizers.FirstOrDefault() is TapGestureRecognizer tap
            && tap.CommandParameter is string tab)
        {
            _vm.SelectTabCommand.Execute(tab);
            UpdateTabUI(tab);
        }
    }

    private void UpdateTabUI(string activeTab)
    {
        var activeBlue = (Color)(Application.Current?.Resources["PrimaryBlue"] ?? Color.FromArgb("#185FA5"));
        var lightBlue = Color.FromArgb("#E6F1FB");

        UpcomingChip.BackgroundColor = activeTab == "Upcoming" ? activeBlue : lightBlue;
        UpcomingChipText.TextColor = activeTab == "Upcoming" ? Colors.White : activeBlue;
        UpcomingChipText.FontAttributes = activeTab == "Upcoming" ? FontAttributes.Bold : FontAttributes.None;

        PastChip.BackgroundColor = activeTab == "Past" ? activeBlue : lightBlue;
        PastChipText.TextColor = activeTab == "Past" ? Colors.White : activeBlue;
        PastChipText.FontAttributes = activeTab == "Past" ? FontAttributes.Bold : FontAttributes.None;

        CancelledChip.BackgroundColor = activeTab == "Cancelled" ? activeBlue : lightBlue;
        CancelledChipText.TextColor = activeTab == "Cancelled" ? Colors.White : activeBlue;
        CancelledChipText.FontAttributes = activeTab == "Cancelled" ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _vm.SearchCommand.Execute(e.NewTextValue);
    }

    private async void OnBookClicked(object sender, EventArgs e)
    {
        if (sender is Button btn) await AnimationHelper.ButtonPressAsync(btn);
        await Shell.Current.GoToAsync(nameof(BookAppointmentPage));
    }
}
