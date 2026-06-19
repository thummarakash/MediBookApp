using MediBook.Pages;

namespace MediBook;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(BookAppointmentPage), typeof(BookAppointmentPage));
        Routing.RegisterRoute(nameof(AppointmentConfirmationPage), typeof(AppointmentConfirmationPage));
        Routing.RegisterRoute(nameof(UploadDocumentPage), typeof(UploadDocumentPage));
        Routing.RegisterRoute(nameof(ClinicDetailPage), typeof(ClinicDetailPage));
        Routing.RegisterRoute(nameof(ContactPage), typeof(ContactPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(ManageClinicsPage), typeof(ManageClinicsPage));
        Routing.RegisterRoute(nameof(ManageDoctorsPage), typeof(ManageDoctorsPage));
        Routing.RegisterRoute(nameof(AdminBookingsPage), typeof(AdminBookingsPage));
        Routing.RegisterRoute(nameof(DoctorsPage), typeof(DoctorsPage));
        Routing.RegisterRoute(nameof(NotificationSettingsPage), typeof(NotificationSettingsPage));
        Routing.RegisterRoute(nameof(SecuritySettingsPage), typeof(SecuritySettingsPage));
        Routing.RegisterRoute(nameof(ChangePasswordPage), typeof(ChangePasswordPage));
        Routing.RegisterRoute(nameof(PrivacyPolicyPage), typeof(PrivacyPolicyPage));
        Routing.RegisterRoute(nameof(TermsConditionsPage), typeof(TermsConditionsPage));
        Routing.RegisterRoute(nameof(ForgotPasswordPage), typeof(ForgotPasswordPage));
        Routing.RegisterRoute(nameof(ResetPasswordPage), typeof(ResetPasswordPage));
        Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
        Routing.RegisterRoute(nameof(PinPage), typeof(PinPage));
    }

    protected override void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        if (CurrentPage != null)
        {
            Shell.SetNavBarIsVisible(CurrentPage, false);
            NavigationPage.SetHasNavigationBar(CurrentPage, false);
        }
    }
}
