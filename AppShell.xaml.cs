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
        Routing.RegisterRoute(nameof(ContactPage), typeof(ContactPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }
}
