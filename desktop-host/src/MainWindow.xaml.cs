using System;
using System.IO;
using System.Windows;

namespace MoatHouseHandover.Host;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await AppWebView.EnsureCoreWebView2Async();

        // Stage 1 scaffold contract: host loads static local web app via relative dev path.
        var indexPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "webapp", "index.html"));

        if (!File.Exists(indexPath))
        {
            MessageBox.Show($"webapp index not found: {indexPath}", "Startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        AppWebView.Source = new Uri(indexPath);

        // Stage 2+/packaging: implement production runtime asset resolution + host<->web IPC bridge.
    }
}
