using System.Windows;

namespace PicPaste;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var mainWindow = new MainWindow();
        mainWindow.WindowState = WindowState.Minimized;
        mainWindow.ShowInTaskbar = false;
    }
}
