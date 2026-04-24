using System;
using System.IO;
using System.Windows;

namespace MoatHouseHandover.Host;

public partial class MainWindow : Window
{
    private HostWebBridge? _hostWebBridge;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var startup = new StartupInitializer().Initialize();
            var indexPath = Path.Combine(startup.RuntimeStatus.AssetRoot, "index.html");

            if (!File.Exists(indexPath))
            {
                throw new InvalidOperationException($"webapp index not found: {indexPath}");
            }

            await AppWebView.EnsureCoreWebView2Async();
            var sessionRepository = new SessionRepository(startup.RuntimeStatus.AccessDatabasePath);
            var sessionService = new SessionService(sessionRepository);
            _hostWebBridge = new HostWebBridge(startup.RuntimeStatus, startup.Logger, sessionService);
            _hostWebBridge.Attach(AppWebView.CoreWebView2);

            AppWebView.Source = new Uri(indexPath);
            Title = $"MOAT HOUSE HANDOVER v2 — {Path.GetFileName(startup.RuntimeStatus.ConfigPath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Host startup failed.\n\n{ex.Message}",
                "Startup error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Close();
        }
    }
}
