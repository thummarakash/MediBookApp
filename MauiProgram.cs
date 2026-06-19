using CommunityToolkit.Maui;
using MediBook.Pages;
using MediBook.Services;
using MediBook.Services.Auth;
using MediBook.Services.Email;
using MediBook.Services.Firebase;
using MediBook.Services.Location;
using MediBook.ViewModels;
using Plugin.Fingerprint;

namespace MediBook;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // ── Services (singletons — also accessible via .Instance for backward compat) ──
        builder.Services.AddSingleton(_ => DatabaseService.Instance);
        builder.Services.AddSingleton(_ => BiometricService.Instance);
        builder.Services.AddSingleton(_ => EmailNotificationService.Instance);
        builder.Services.AddSingleton(_ => GoogleAuthService.Instance);
        builder.Services.AddSingleton(_ => SessionService.Instance);
        builder.Services.AddSingleton(_ => FirebaseAuthService.Instance);
        builder.Services.AddSingleton(_ => FirestoreService.Instance);
        builder.Services.AddSingleton(_ => FirebaseStorageService.Instance);
        builder.Services.AddSingleton(_ => FcmService.Instance);
        builder.Services.AddSingleton(_ => SmtpEmailService.Instance);
        builder.Services.AddSingleton(_ => LocationService.Instance);

        // ── ViewModels ────────────────────────────────────────────────────────
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<AppointmentsViewModel>();
        builder.Services.AddTransient<BookAppointmentViewModel>();
        builder.Services.AddTransient<DoctorsViewModel>();
        builder.Services.AddTransient<DocumentsViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<AdminDashboardViewModel>();
        builder.Services.AddTransient<AdminBookingsViewModel>();
        builder.Services.AddTransient<ManageClinicsViewModel>();
        builder.Services.AddTransient<ManageDoctorsViewModel>();

        // ── Pages ─────────────────────────────────────────────────────────────
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<AppointmentsPage>();
        builder.Services.AddTransient<BookAppointmentPage>();
        builder.Services.AddTransient<DoctorsPage>();
        builder.Services.AddTransient<DocumentsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<AdminDashboardPage>();
        builder.Services.AddTransient<AdminBookingsPage>();
        builder.Services.AddTransient<ManageClinicsPage>();
        builder.Services.AddTransient<ManageDoctorsPage>();

        // ── Android platform customisations ───────────────────────────────────
#if ANDROID
        Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
        });
        Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
        {
            handler.PlatformView.Background = null;
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
        });

        // Register Plugin.Fingerprint with the Android activity
        CrossFingerprint.SetCurrentActivityResolver(() =>
            Microsoft.Maui.ApplicationModel.Platform.CurrentActivity!);
#endif

        return builder.Build();
    }
}
