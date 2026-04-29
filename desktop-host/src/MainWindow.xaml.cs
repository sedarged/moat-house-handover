using System;
using System.IO;
using System.Windows;
using Microsoft.Web.WebView2.Core;

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

            // Map a virtual hostname for the attachments folder so that WebView2 can load
            // attachment images. Without this, WebView2 blocks cross-directory file:// requests
            // (Chromium same-origin policy for file:// sources applies from WebView2 SDK 1.0.1343+).
            if (Directory.Exists(startup.RuntimeStatus.AttachmentsRoot))
            {
                AppWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    AttachmentService.AttachmentsVirtualHostName,
                    startup.RuntimeStatus.AttachmentsRoot,
                    CoreWebView2HostResourceAccessKind.Allow);
            }

            var sessionRepository = new SessionRepository(startup.RuntimeStatus.AccessDatabasePath);
            var departmentRepository = new DepartmentRepository(startup.RuntimeStatus.AccessDatabasePath);
            var attachmentRepository = new AttachmentRepository(startup.RuntimeStatus.AccessDatabasePath);
            var budgetRepository = new BudgetRepository(startup.RuntimeStatus.AccessDatabasePath);
            var previewRepository = new PreviewRepository(startup.RuntimeStatus.AccessDatabasePath);
            var emailProfileRepository = new EmailProfileRepository(startup.RuntimeStatus.AccessDatabasePath);
            var auditLogRepository = new AuditLogRepository(startup.RuntimeStatus.AccessDatabasePath);

            var auditLogService = new AuditLogService(auditLogRepository, startup.Logger);
            var sessionService = new SessionService(sessionRepository, auditLogService);
            var departmentService = new DepartmentService(departmentRepository, auditLogService);
            var attachmentService = new AttachmentService(attachmentRepository, auditLogService, startup.Config);
            var budgetService = new BudgetService(budgetRepository, auditLogService);
            var previewService = new PreviewService(previewRepository);
            var reportService = new ReportService(previewService, auditLogService, startup.Config);
            var emailProfileService = new EmailProfileService(emailProfileRepository);
            var outlookDraftService = new OutlookDraftService();
            var sendPackageService = new SendPackageService(previewService, reportService, emailProfileService, outlookDraftService, auditLogService);
            var diagnosticsService = new DiagnosticsService(startup.RuntimeStatus, startup.Config);
            var fileDialogService = new FileDialogService();

            _hostWebBridge = new HostWebBridge(
                startup.RuntimeStatus,
                startup.Logger,
                sessionService,
                departmentService,
                attachmentService,
                budgetService,
                previewService,
                reportService,
                emailProfileService,
                sendPackageService,
                diagnosticsService,
                auditLogService,
                fileDialogService);
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
