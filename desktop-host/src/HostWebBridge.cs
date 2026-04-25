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
    private readonly FileDialogService _fileDialogService;

    public HostWebBridge(
        HostRuntimeStatus runtimeStatus,
        BootstrapLogger logger,
        SessionService sessionService,
        DepartmentService departmentService,
        AttachmentService attachmentService,
        FileDialogService fileDialogService)
    {
        _runtimeStatus = runtimeStatus;
        _logger = logger;
        _sessionService = sessionService;
        _departmentService = departmentService;
        _attachmentService = attachmentService;
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

                case "shell.openOutputFolder":
                    OpenFolder(_runtimeStatus.ReportsOutputRoot);
                    SendResponse(webView, request.RequestId, true, null, new { opened = _runtimeStatus.ReportsOutputRoot });
                    break;

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

                default:
                    SendResponse(webView, request.RequestId, false, $"Unsupported bridge message: {request.Type}", null);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Log($"Bridge request failed ({request.Type}): {ex}");
            SendResponse(webView, request.RequestId, false, ex.Message, null);
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
        _logger.Log($"Bridge request open folder: {path}");
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{path}\"",
            UseShellExecute = true
        });
    }

    private static void SendResponse(CoreWebView2 webView, string? requestId, bool success, string? error, object? payload)
    {
        var response = new BridgeResponse(requestId, success, error, payload);
        webView.PostWebMessageAsJson(JsonSerializer.Serialize(response, ResponseJsonOptions));
    }
}

public sealed record BridgeRequest(string? RequestId, string Type, JsonElement? Payload);

public sealed record BridgeResponse(string? RequestId, bool Success, string? Error, object? Payload);
