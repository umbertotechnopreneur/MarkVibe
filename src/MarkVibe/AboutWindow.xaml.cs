using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using MarkVibe.Controls;

namespace MarkVibe;

public partial class AboutWindow : ChildWindow
{
    private const string RepositoryUrl = "https://github.com/umbertotechnopreneur/MarkVibe";
    public AboutWindow()
    {
        InitializeComponent();
        var assembly = typeof(AboutWindow).Assembly;
        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString() ?? "Development";
        VersionText.Text = "Version " + version.Split('+')[0];
        var built = assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(item => item.Key == "BuildTimestamp")?.Value;
        BuiltText.Text = string.IsNullOrEmpty(built) ? "Local development build · OpenSilver and WebView2" : "Built " + built + " · OpenSilver and WebView2";
        var root = Application.Current.RootVisual as FrameworkElement;
        Width = Math.Max(270, Math.Min(620, (root?.ActualWidth ?? 652) - 32));
        Height = Math.Max(270, Math.Min(610, (root?.ActualHeight ?? 658) - 48));
        Loaded += (_, _) => ButtonIcons.Decorate(this);
    }
    private static void Open(string url) => OpenSilver.Interop.ExecuteJavaScriptVoid("window.open($0, '_blank', 'noopener,noreferrer')", url);
    private void Author(object sender, RoutedEventArgs e) => Open("https://umbertogiacobbi.biz");
    private void Repository(object sender, RoutedEventArgs e) => Open(RepositoryUrl);
    private void License(object sender, RoutedEventArgs e) => Open(RepositoryUrl + "/blob/master/LICENSE");
    private void Manifesto(object sender, RoutedEventArgs e) => Open("https://umbertogiacobbi.biz/vibeware/manifesto");
    private void Dismiss(object sender, RoutedEventArgs e) => Close();
}
