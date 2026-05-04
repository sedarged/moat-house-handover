using System;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;

namespace MoatHouseHandover.Host;

public sealed class HostWebBridge
{
    private static readonly JsonSerializerOptions RequestJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions ResponseJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly HostRuntimeStatus _runtimeStatus;
    private readonly BootstrapLogger _logger;
    private readonly SessionService _sessionService;
    private readonly DepartmentService _departmentService;
    private readonly AttachmentService _attachmentService;
    private readonly BudgetService _budgetService;
    private readonly PreviewService _previewService;
    private readonly ReportService _reportService;
    private readonly EmailProfileService _emailProfileService;
    private readonly SendPackageService _sendPackageService;
    private readonly DiagnosticsService _diagnosticsService;
    private readonly AuditLogService _auditLogService;
    private readonly FileDialogService _fileDialogService;

    public HostWebBridge(
        HostRuntimeStatus runtimeStatus,
        BootstrapLogger logger,
        SessionService sessionService,
        DepartmentService departmentService,
        AttachmentService attachmentService,
        BudgetService budgetService,
        PreviewService previewService,
        ReportService reportService,
        EmailProfileService emailProfileService,
        SendPackageService sendPackageService,
        DiagnosticsService diagnosticsService,
        AuditLogService auditLogService,
        FileDialogService fileDialogService)
    {
        _runtimeStatus = runtimeStatus;
        _logger = logger;
        _sessionService = sessionService;
        _departmentService = departmentService;
        _attachmentService = attachmentService;
        _budgetService = budgetService;
        _previewService = previewService;
        _reportService = reportService;
        _emailProfileService = emailProfileService;
        _sendPackageService = sendPackageService;
        _diagnosticsService = diagnosticsService;
        _auditLogService = auditLogService;
        _fileDialogService = fileDialogService;
    }

    public void Attach(CoreWebView2 webView)
    {
        webView.WebMessageReceived += OnWebMessageReceived;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (sender is not CoreWebView2 webView)
        {
            return;
        }

        BridgeRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<BridgeRequest>(e.WebMessageAsJson, RequestJsonOptions);
        }
        catch
        {
            SendResponse(webView, null, false, "Invalid bridge request JSON.", null);
            return;
        }

        if (request is null || string.IsNullOrWhiteSpace(request.Type))
        {
            SendResponse(webView, request?.RequestId, false, "Bridge request type is required.", null);
            return;
        }

        try
        {
            switch (request.Type)
            {
                case "runtime.getStatus":
                    SendResponse(webView, request.RequestId, true, null, _runtimeStatus);
                    break;

                case "diagnostics.run":
                {
                    var payload = request.Payload is null
                        ? new DiagnosticsRunRequest(null)
                        : DeserializePayload<DiagnosticsRunRequest>(request.Payload);
                    var result = _diagnosticsService.Run(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "audit.listRecent":
                {
                    var payload = request.Payload is null
                        ? new AuditListRecentRequest(null)
                        : DeserializePayload<AuditListRecentRequest>(request.Payload);
                    var result = _auditLogService.ListRecent(payload);
                    SendResponse(webView, request.RequestId, result.Success, result.Error, result);
                    break;
                }

                case "audit.listForSession":
                {
                    var payload = DeserializePayload<AuditListForSessionRequest>(request.Payload);
                    var result = _auditLogService.ListForSession(payload);
                    SendResponse(webView, request.RequestId, result.Success, result.Error, result);
                    break;
                }

                case "shell.openOutputFolder":
                case "shell.openReportsFolder":
                {
                    var payload = request.Payload is null
                        ? new ReportsFolderRequest(null)
                        : DeserializePayload<ReportsFolderRequest>(request.Payload);
                    var folder = _reportService.ResolveReportsFolder(payload);
                    OpenFolder(folder.OpenedPath);
                    SendResponse(webView, request.RequestId, true, null, folder);
                    break;
                }
                case "shell.openLogsFolder":
                {
                    OpenFolder(_runtimeStatus.LogRoot);
                    SendResponse(webView, request.RequestId, true, null, new { openedPath = _runtimeStatus.LogRoot });
                    break;
                }

                case "session.open":
                {
                    var payload = DeserializePayload<SessionOpenRequest>(request.Payload);
                    var result = _sessionService.OpenSession(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "session.createBlank":
                {
                    var payload = DeserializePayload<SessionCreateBlankRequest>(request.Payload);
                    var result = _sessionService.CreateBlankSession(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "session.clearDay":
                {
                    var payload = DeserializePayload<SessionClearDayRequest>(request.Payload);
                    var result = _sessionService.ClearDay(payload);
                    SendResponse(webView, request.RequestId, true, null, new { session = result });
                    break;
                }


                case "session.list":
                {
                    var payload = request.Payload is null
                        ? new SessionListFilters(null, null, null, null)
                        : DeserializePayload<SessionListFilters>(request.Payload);
                    var result = _sessionService.ListSessions(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "session.openById":
                {
                    var payload = DeserializePayload<SessionOpenByIdRequest>(request.Payload);
                    var result = _sessionService.OpenSessionById(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "department.load":
                {
                    var payload = DeserializePayload<DepartmentLoadRequest>(request.Payload);
                    var result = _departmentService.LoadDepartment(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "department.save":
                {
                    var payload = DeserializePayload<DepartmentSaveRequest>(request.Payload);
                    var result = _departmentService.SaveDepartment(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "file.pickFile":
                {
                    var payload = request.Payload is null
                        ? new FilePickRequest(null, null)
                        : DeserializePayload<FilePickRequest>(request.Payload);
                    var result = _fileDialogService.PickFile(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "attachment.list":
                {
                    var payload = DeserializePayload<AttachmentListRequest>(request.Payload);
                    var result = _attachmentService.ListAttachments(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "attachment.add":
                {
                    var payload = DeserializePayload<AttachmentAddRequest>(request.Payload);
                    var result = _attachmentService.AddAttachment(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "attachment.remove":
                {
                    var payload = DeserializePayload<AttachmentRemoveRequest>(request.Payload);
                    var result = _attachmentService.RemoveAttachment(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "attachment.openViewer":
                {
                    var payload = DeserializePayload<AttachmentViewerRequest>(request.Payload);
                    var result = _attachmentService.GetViewerPayload(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "budget.load":
                {
                    var payload = DeserializePayload<BudgetLoadRequest>(request.Payload);
                    var result = _budgetService.LoadBudget(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "budget.save":
                {
                    var payload = DeserializePayload<BudgetSaveRequest>(request.Payload);
                    var result = _budgetService.SaveBudget(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "budget.recalculate":
                {
                    var payload = DeserializePayload<BudgetRecalculateRequest>(request.Payload);
                    var result = _budgetService.Recalculate(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "dashboard.budgetSummary":
                {
                    var payload = DeserializePayload<DashboardBudgetSummaryRequest>(request.Payload);
                    var result = _budgetService.LoadBudgetSummary(payload.SessionId);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "preview.load":
                {
                    var payload = DeserializePayload<PreviewLoadRequest>(request.Payload);
                    var result = _previewService.LoadPreview(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "reports.generateHandover":
                {
                    var payload = DeserializePayload<ReportGenerateRequest>(request.Payload);
                    var result = _reportService.GenerateHandoverReport(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "reports.generateBudget":
                {
                    var payload = DeserializePayload<ReportGenerateRequest>(request.Payload);
                    var result = _reportService.GenerateBudgetReport(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "reports.generateAll":
                {
                    var payload = DeserializePayload<ReportGenerateRequest>(request.Payload);
                    var result = _reportService.GenerateAllReports(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "emailProfile.loadForShift":
                {
                    var payload = DeserializePayload<EmailProfileLoadRequest>(request.Payload);
                    var result = _emailProfileService.LoadActiveForShift(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "send.preparePackage":
                {
                    var payload = DeserializePayload<SendPreparePackageRequest>(request.Payload);
                    var result = _sendPackageService.PreparePackage(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                case "send.createOutlookDraft":
                {
                    var payload = DeserializePayload<SendCreateOutlookDraftRequest>(request.Payload);
                    var result = _sendPackageService.CreateOutlookDraft(payload);
                    SendResponse(webView, request.RequestId, true, null, result);
                    break;
                }

                default:
                    SendResponse(webView, request.RequestId, false, $"Unsupported bridge message: {request.Type}", null);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"Bridge request failed ({request.Type}): {ex}");
            SendResponse(webView, request.RequestId, false, ex.Message, new { errorType = ex.GetType().Name, action = request.Type });
        }
    }

    private static T DeserializePayload<T>(JsonElement? payload)
    {
        if (payload is null)
        {
            throw new InvalidOperationException("Bridge payload is required.");
        }

        var value = payload.Value.Deserialize<T>(RequestJsonOptions);
        if (value is null)
        {
            throw new InvalidOperationException("Bridge payload could not be parsed.");
        }

        return value;
    }

    private void OpenFolder(string path)
    {
        if (!System.IO.Directory.Exists(path))
        {
            throw new InvalidOperationException($"Folder does not exist: {path}");
        }

        _logger.Log($"Bridge request open folder: {path}");
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{path}\"",
                UseShellExecute = true
            });
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private static void SendResponse(CoreWebView2 webView, string? requestId, bool success, string? error, object? payload)
    {
        var response = new BridgeResponse(requestId, success, error, payload);
        webView.PostWebMessageAsJson(JsonSerializer.Serialize(response, ResponseJsonOptions));
    }

    private sealed record BridgeRequest(string? RequestId, string Type, JsonElement? Payload);

    private sealed record BridgeResponse(string? RequestId, bool Success, string? Error, object? Payload);
}
