namespace MediBook;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Always force Light theme as requested
        UserAppTheme = AppTheme.Light;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }
}
