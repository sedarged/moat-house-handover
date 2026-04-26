using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MoatHouseHandover.Host;

public sealed class ReportService
{
    private readonly PreviewService _previewService;
    private readonly string _reportsRoot;

    public ReportService(PreviewService previewService, HostConfig config)
    {
        _previewService = previewService;
        _reportsRoot = Path.GetFullPath(config.ReportsOutputRoot);
    }

    public ReportGenerateResult GenerateHandoverReport(ReportGenerateRequest request)
    {
        var normalizedUser = NormalizeUser(request.UserName);
        var preview = LoadPreview(request.SessionId);
        var reportDirectory = EnsureSessionReportDirectory(preview.Session);

        var fileName = BuildFileName("Handover", preview.Session);
        var fullPath = Path.Combine(reportDirectory, fileName);
        File.WriteAllText(fullPath, BuildHandoverHtml(preview), Encoding.UTF8);

        return BuildResult("handover", normalizedUser, preview.Session, new[] { fullPath });
    }

    public ReportGenerateResult GenerateBudgetReport(ReportGenerateRequest request)
    {
        var normalizedUser = NormalizeUser(request.UserName);
        var preview = LoadPreview(request.SessionId);
        var reportDirectory = EnsureSessionReportDirectory(preview.Session);

        var fileName = BuildFileName("Budget", preview.Session);
        var fullPath = Path.Combine(reportDirectory, fileName);
        File.WriteAllText(fullPath, BuildBudgetHtml(preview), Encoding.UTF8);

        return BuildResult("budget", normalizedUser, preview.Session, new[] { fullPath });
    }

    public ReportGenerateResult GenerateAllReports(ReportGenerateRequest request)
    {
        var normalizedUser = NormalizeUser(request.UserName);
        var preview = LoadPreview(request.SessionId);
        var reportDirectory = EnsureSessionReportDirectory(preview.Session);

        var handoverPath = Path.Combine(reportDirectory, BuildFileName("Handover", preview.Session));
        var budgetPath = Path.Combine(reportDirectory, BuildFileName("Budget", preview.Session));

        File.WriteAllText(handoverPath, BuildHandoverHtml(preview), Encoding.UTF8);
        File.WriteAllText(budgetPath, BuildBudgetHtml(preview), Encoding.UTF8);

        return BuildResult("all", normalizedUser, preview.Session, new[] { handoverPath, budgetPath });
    }

    public ReportsFolderResult ResolveReportsFolder(ReportsFolderRequest request)
    {
        if (request.SessionId.HasValue && request.SessionId.Value > 0)
        {
            var preview = LoadPreview(request.SessionId.Value);
            var path = EnsureSessionReportDirectory(preview.Session);
            return new ReportsFolderResult(path);
        }

        Directory.CreateDirectory(_reportsRoot);
        return new ReportsFolderResult(_reportsRoot);
    }

    private PreviewPayload LoadPreview(long sessionId)
    {
        return _previewService.LoadPreview(new PreviewLoadRequest(sessionId));
    }

    private string EnsureSessionReportDirectory(PreviewSessionHeader header)
    {
        var shift = SanitizeSegment(header.ShiftCode);
        var shiftDate = SanitizeSegment(header.ShiftDate);
        var directory = Path.Combine(_reportsRoot, shift, shiftDate);
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string BuildFileName(string reportPrefix, PreviewSessionHeader header)
    {
        var safePrefix = SanitizeSegment(reportPrefix);
        var safeShift = SanitizeSegment(header.ShiftCode);
        var safeDate = SanitizeSegment(header.ShiftDate);
        var safeSession = SanitizeSegment($"Session{header.SessionId.ToString(CultureInfo.InvariantCulture)}");
        return $"{safePrefix}_{safeShift}_{safeDate}_{safeSession}.html";
    }

    private static ReportGenerateResult BuildResult(string reportType, string userName, PreviewSessionHeader session, IReadOnlyList<string> filePaths)
    {
        return new ReportGenerateResult(
            Success: true,
            ReportType: reportType,
            GeneratedAt: DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            GeneratedBy: userName,
            SessionId: session.SessionId,
            ShiftCode: session.ShiftCode,
            ShiftDate: session.ShiftDate,
            FilePaths: filePaths);
    }

    private static string BuildHandoverHtml(PreviewPayload preview)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>MOAT Handover Report</title>");
        sb.Append("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:20px;}table{border-collapse:collapse;width:100%;margin-top:12px;}th,td{border:1px solid #ccc;padding:6px;text-align:left;}h1,h2{margin-bottom:8px;}p{margin:4px 0;}.small{color:#555;font-size:12px;}</style>");
        sb.Append("</head><body>");
        sb.Append("<h1>MOAT HOUSE HANDOVER - Handover Report</h1>");
        sb.Append($"<p><strong>Session:</strong> {Encode(preview.Session.SessionId.ToString(CultureInfo.InvariantCulture))}</p>");
        sb.Append($"<p><strong>Shift:</strong> {Encode(preview.Session.ShiftCode)} | <strong>Date:</strong> {Encode(preview.Session.ShiftDate)}</p>");
        sb.Append($"<p><strong>Status:</strong> {Encode(preview.Session.SessionStatus)}</p>");
        sb.Append($"<p class=\"small\">Created: {Encode(preview.Session.CreatedAt)} by {Encode(preview.Session.CreatedBy)} | Updated: {Encode(preview.Session.UpdatedAt)} by {Encode(preview.Session.UpdatedBy)}</p>");

        sb.Append("<h2>Department Summary</h2><table><thead><tr><th>Department</th><th>Status</th><th>Downtime</th><th>Efficiency</th><th>Yield</th><th>Attachments</th><th>Notes</th><th>Updated</th></tr></thead><tbody>");
        foreach (var dept in preview.Departments)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{Encode(dept.DeptName)}</td>");
            sb.Append($"<td>{Encode(dept.DeptStatus)}</td>");
            sb.Append($"<td>{Encode(dept.DowntimeMin?.ToString(CultureInfo.InvariantCulture) ?? string.Empty)}</td>");
            sb.Append($"<td>{Encode(FormatNumber(dept.EfficiencyPct))}</td>");
            sb.Append($"<td>{Encode(FormatNumber(dept.YieldPct))}</td>");
            sb.Append($"<td>{Encode(dept.AttachmentCount.ToString(CultureInfo.InvariantCulture))}</td>");
            sb.Append($"<td>{Encode(dept.Notes)}</td>");
            sb.Append($"<td>{Encode(dept.UpdatedAt ?? string.Empty)} {Encode(dept.UpdatedBy ?? string.Empty)}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");

        sb.Append("<h2>Attachment Summary</h2><table><thead><tr><th>Department</th><th>Count</th><th>Files</th></tr></thead><tbody>");
        foreach (var group in preview.AttachmentSummary)
        {
            var names = new List<string>();
            foreach (var item in group.Attachments)
            {
                names.Add($"#{item.SequenceNo.ToString(CultureInfo.InvariantCulture)} {item.DisplayName}");
            }
            var joined = string.Join("; ", names);
            sb.Append("<tr>");
            sb.Append($"<td>{Encode(group.DeptName)}</td>");
            sb.Append($"<td>{Encode(group.AttachmentCount.ToString(CultureInfo.InvariantCulture))}</td>");
            sb.Append($"<td>{Encode(joined)}</td>");
            sb.Append("</tr>");
        }
        sb.Append("</tbody></table>");

        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string BuildBudgetHtml(PreviewPayload preview)
    {
        var sb = new StringBuilder();
        sb.Append("<!doctype html><html><head><meta charset=\"utf-8\"><title>MOAT Budget Report</title>");
        sb.Append("<style>body{font-family:Segoe UI,Arial,sans-serif;margin:20px;}table{border-collapse:collapse;width:100%;margin-top:12px;}th,td{border:1px solid #ccc;padding:6px;text-align:left;}h1,h2{margin-bottom:8px;}p{margin:4px 0;}.small{color:#555;font-size:12px;}</style>");
        sb.Append("</head><body>");
        sb.Append("<h1>MOAT HOUSE HANDOVER - Budget Report</h1>");
        sb.Append($"<p><strong>Session:</strong> {Encode(preview.Session.SessionId.ToString(CultureInfo.InvariantCulture))}</p>");
        sb.Append($"<p><strong>Shift:</strong> {Encode(preview.Session.ShiftCode)} | <strong>Date:</strong> {Encode(preview.Session.ShiftDate)}</p>");
        sb.Append($"<p><strong>Status:</strong> {Encode(preview.BudgetSummary.Status)}</p>");
        sb.Append($"<p><strong>Totals:</strong> Planned {Encode(preview.BudgetSummary.PlannedTotal.ToString("0.##", CultureInfo.InvariantCulture))}, Used {Encode(preview.BudgetSummary.UsedTotal.ToString("0.##", CultureInfo.InvariantCulture))}, Variance {Encode(preview.BudgetSummary.VarianceTotal.ToString("0.##", CultureInfo.InvariantCulture))}</p>");
        sb.Append($"<p class=\"small\">Last updated: {Encode(preview.BudgetSummary.LastUpdatedAt ?? string.Empty)} by {Encode(preview.BudgetSummary.LastUpdatedBy ?? string.Empty)}</p>");

        sb.Append("<h2>Budget Rows</h2><table><thead><tr><th>Department</th><th>Planned</th><th>Used</th><th>Variance</th><th>Status</th><th>Reason</th><th>Updated</th></tr></thead><tbody>");

        foreach (var row in preview.BudgetRows)
        {
            sb.Append("<tr>");
            sb.Append($"<td>{Encode(row.DeptName)}</td>");
            sb.Append($"<td>{Encode(FormatNumber(row.PlannedQty))}</td>");
            sb.Append($"<td>{Encode(FormatNumber(row.UsedQty))}</td>");
            sb.Append($"<td>{Encode(row.Variance.ToString("0.##", CultureInfo.InvariantCulture))}</td>");
            sb.Append($"<td>{Encode(row.Status)}</td>");
            sb.Append($"<td>{Encode(row.ReasonText)}</td>");
            sb.Append($"<td>{Encode(row.UpdatedAt ?? string.Empty)} {Encode(row.UpdatedBy ?? string.Empty)}</td>");
            sb.Append("</tr>");
        }

        sb.Append("</tbody></table>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    private static string SanitizeSegment(string value)
    {
        var input = string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }

        return sb.ToString().Replace(' ', '_');
    }

    private static string NormalizeUser(string userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            return userName.Trim();
        }

        return Environment.UserName;
    }

    private static string Encode(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("'", "&#39;", StringComparison.Ordinal);
    }

    private static string FormatNumber(double? value)
    {
        return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
    }
}
