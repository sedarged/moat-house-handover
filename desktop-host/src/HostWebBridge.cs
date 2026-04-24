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

    public HostWebBridge(HostRuntimeStatus runtimeStatus, BootstrapLogger logger)
    {
        _runtimeStatus = runtimeStatus;
        _logger = logger;
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

        switch (request.Type)
        {
            case "runtime.getStatus":
                SendResponse(webView, request.RequestId, true, null, _runtimeStatus);
                break;

            case "shell.openOutputFolder":
                OpenFolder(_runtimeStatus.ReportsOutputRoot);
                SendResponse(webView, request.RequestId, true, null, new { opened = _runtimeStatus.ReportsOutputRoot });
                break;

            case "file.pickFile":
                SendResponse(webView, request.RequestId, false, "Not implemented in Stage 2A.", null);
                break;

            default:
                SendResponse(webView, request.RequestId, false, $"Unsupported bridge message: {request.Type}", null);
                break;
        }
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
