using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MoatHouseHandover.Host;

public sealed class SendPackageService
{
    private readonly PreviewService _previewService;
    private readonly ReportService _reportService;
    private readonly EmailProfileService _emailProfileService;
    private readonly OutlookDraftService _outlookDraftService;
    private readonly AuditLogService _auditLogService;

    public SendPackageService(
        PreviewService previewService,
        ReportService reportService,
        EmailProfileService emailProfileService,
        OutlookDraftService outlookDraftService,
        AuditLogService auditLogService)
    {
        _previewService = previewService;
        _reportService = reportService;
        _emailProfileService = emailProfileService;
        _outlookDraftService = outlookDraftService;
        _auditLogService = auditLogService;
    }

    public SendPreparePackageResult PreparePackage(SendPreparePackageRequest request)
    {
        var package = BuildPackage(request.SessionId, request.UserName);
        _auditLogService.BestEffortLog(
            actionType: "send.preparePackage",
            entityType: "Send",
            entityKey: AuditLogService.BuildSessionKey(request.SessionId),
            userName: NormalizeUser(request.UserName),
            details: new { sessionId = request.SessionId, readinessStatus = package.ReadinessStatus, validationCount = package.ValidationMessages.Count });

        return new SendPreparePackageResult(Success: package.IsReady, Package: package);
    }

    public SendCreateOutlookDraftResult CreateOutlookDraft(SendCreateOutlookDraftRequest request)
    {
        var package = BuildPackage(request.SessionId, request.UserName);
        if (!package.IsReady)
        {
            var invalidResult = new OutlookDraftResult(false, "Send package validation failed. Draft was not created.", null, package.GeneratedAt, 0);
            _auditLogService.BestEffortLog(
                actionType: "send.createOutlookDraft result",
                entityType: "Send",
                entityKey: AuditLogService.BuildSessionKey(request.SessionId),
                userName: NormalizeUser(request.UserName),
                details: new { sessionId = request.SessionId, draftCreated = false, message = invalidResult.Message, validationCount = package.ValidationMessages.Count });

            return new SendCreateOutlookDraftResult(
                Success: false,
                Package: package,
                Draft: invalidResult);
        }

        var draft = _outlookDraftService.CreateDraft(new OutlookDraftRequest(
            ToList: package.ToList,
            CcList: package.CcList,
            Subject: package.Subject,
            Body: package.Body,
            AttachmentPaths: package.AttachmentPaths));

        _auditLogService.BestEffortLog(
            actionType: "send.createOutlookDraft result",
            entityType: "Send",
            entityKey: AuditLogService.BuildSessionKey(request.SessionId),
            userName: NormalizeUser(request.UserName),
            details: new { sessionId = request.SessionId, draftCreated = draft.DraftCreated, message = draft.Message, attachmentCount = draft.AttachmentCount });

        return new SendCreateOutlookDraftResult(
            Success: draft.DraftCreated,
            Package: package,
            Draft: draft);
    }

    private SendPackagePayload BuildPackage(long sessionId, string userName)
    {
        var validation = new List<string>();
        if (sessionId <= 0)
        {
            validation.Add("SessionId is required.");
            return BuildFallbackInvalid(sessionId, userName, validation);
        }

        PreviewPayload? preview = null;
        try
        {
            preview = _previewService.LoadPreview(new PreviewLoadRequest(sessionId));
        }
        catch (Exception ex)
        {
            validation.Add($"Session/preview validation failed: {ex.Message}");
        }

        if (preview is null)
        {
            return BuildFallbackInvalid(sessionId, userName, validation);
        }

        List<string> reportPaths = new();
        try
        {
            var generated = _reportService.GenerateAllReports(new ReportGenerateRequest(sessionId, userName));
            reportPaths = generated.FilePaths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }
        catch (Exception ex)
        {
            validation.Add($"Report generation failed: {ex.Message}");
        }

        if (reportPaths.Count == 0)
        {
            validation.Add("No report files are available for attachment. Generate reports before creating a draft.");
        }

        foreach (var path in reportPaths)
        {
            if (!File.Exists(path))
            {
                validation.Add($"A generated report file is missing on disk: {path}");
            }
        }

        EmailProfilePayload? profile = null;
        try
        {
            profile = _emailProfileService.LoadActiveForShift(new EmailProfileLoadRequest(preview.Session.ShiftCode));
        }
        catch (Exception ex)
        {
            validation.Add($"Email profile is missing or inactive for shift {preview.Session.ShiftCode}: {ex.Message}");
        }

        var toList = profile?.ToList?.Trim() ?? string.Empty;
        var ccList = profile?.CcList?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(toList) && string.IsNullOrWhiteSpace(ccList))
        {
            validation.Add("At least one recipient is required (To or CC).");
        }

        var generatedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture);
        var generatedBy = NormalizeUser(userName);

        var tokens = BuildTokens(preview, reportPaths);
        var subject = ApplyTokens(profile?.SubjectTemplate, tokens);
        var body = ApplyTokens(profile?.BodyTemplate, tokens);

        var isReady = validation.Count == 0;
        var status = isReady ? "ready" : "invalid";

        return new SendPackagePayload(
            SessionId: preview.Session.SessionId,
            ShiftCode: preview.Session.ShiftCode,
            ShiftDate: preview.Session.ShiftDate,
            EmailProfileKey: profile?.EmailProfileKey ?? string.Empty,
            ToList: toList,
            CcList: ccList,
            Subject: subject,
            Body: body,
            AttachmentPaths: reportPaths,
            GeneratedAt: generatedAt,
            GeneratedBy: generatedBy,
            IsReady: isReady,
            ReadinessStatus: status,
            ValidationMessages: validation);
    }

    private static SendPackagePayload BuildFallbackInvalid(long sessionId, string userName, IReadOnlyList<string> validation)
    {
        return new SendPackagePayload(
            SessionId: sessionId,
            ShiftCode: string.Empty,
            ShiftDate: string.Empty,
            EmailProfileKey: string.Empty,
            ToList: string.Empty,
            CcList: string.Empty,
            Subject: string.Empty,
            Body: string.Empty,
            AttachmentPaths: Array.Empty<string>(),
            GeneratedAt: DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            GeneratedBy: NormalizeUser(userName),
            IsReady: false,
            ReadinessStatus: "invalid",
            ValidationMessages: validation);
    }

    private static Dictionary<string, string> BuildTokens(PreviewPayload preview, IReadOnlyList<string> reportPaths)
    {
        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ShiftCode"] = preview.Session.ShiftCode,
            ["ShiftDate"] = preview.Session.ShiftDate,
            ["SessionId"] = preview.Session.SessionId.ToString(CultureInfo.InvariantCulture),
            ["ReportPaths"] = string.Join(Environment.NewLine, reportPaths)
        };
    }

    private static string ApplyTokens(string? template, IReadOnlyDictionary<string, string> tokens)
    {
        var value = string.IsNullOrWhiteSpace(template)
            ? "MOAT Handover {ShiftCode} {ShiftDate} Session {SessionId}"
            : template;

        foreach (var token in tokens)
        {
            value = value.Replace($"{{{token.Key}}}", token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        return value;
    }

    private static string NormalizeUser(string userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            return userName.Trim();
        }

        return Environment.UserName;
    }
}
