using System.Windows;

namespace MarkVibe.Windows;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var options = DesktopOptions.Parse(e.Args);
        MainWindow = new DesktopWindow(options); MainWindow.Show();
    }
}
