namespace MediBook;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Always force Light theme as requested
        UserAppTheme = AppTheme.Light;

        Task.Run(async () =>
        {
            try
            {
                await Services.DatabaseService.Instance.SeedDefaultAdminAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MediBook] Startup seed failed: {ex.Message}");
            }
        });
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
