using System.Windows;

namespace MarkVibe;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Startup += (_, _) => RootVisual = new MainPage();
    }
}
