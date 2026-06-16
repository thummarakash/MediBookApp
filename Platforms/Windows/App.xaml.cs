using Microsoft.UI.Xaml;

namespace MediBook.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => MediBook.MauiProgram.CreateMauiApp();
}
