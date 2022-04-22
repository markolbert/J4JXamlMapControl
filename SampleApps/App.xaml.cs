using Microsoft.UI.Xaml;

namespace J4JSoftware.XamlMapControl.WinUIApp;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        InitializeComponent();
    }

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}