using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MoatHouseHandover.Host;

public sealed class AttachmentService
{
    private readonly IAttachmentRepository _repository;
    private readonly AuditLogService _auditLogService;
    private readonly string _attachmentsRootFullPath;

    public AttachmentService(IAttachmentRepository repository, AuditLogService auditLogService, HostConfig config)
    {
        _repository = repository;
        _auditLogService = auditLogService;
        _attachmentsRootFullPath = EnsureTrailingDirectorySeparator(Path.GetFullPath(config.AttachmentsRoot));
    }

    public AttachmentListResult ListAttachments(AttachmentListRequest request)
    {
        ValidateSessionAndDept(request.SessionId, request.DeptRecordId, request.DeptName);
        return _repository.ListAttachments(request.SessionId, request.DeptRecordId, NormalizeDeptName(request.DeptName));
    }

    public AttachmentListResult AddAttachment(AttachmentAddRequest request)
    {
        ValidateSessionAndDept(request.SessionId, request.DeptRecordId, request.DeptName);

        var sourceFilePath = (request.SourceFilePath ?? string.Empty).Trim();
        if (!File.Exists(sourceFilePath))
        {
            throw new InvalidOperationException($"Source attachment file was not found: {sourceFilePath}");
        }

        var safeDisplayName = NormalizeDisplayName(request.DisplayName, sourceFilePath);
        var shiftDatePath = ResolveShiftDateFolder(request.SessionId);
        var deptFolderName = ToSafePathSegment(request.DeptName);
        var targetDirectory = Path.Combine(_attachmentsRootFullPath, request.SessionId.ToString(CultureInfo.InvariantCulture), shiftDatePath, deptFolderName);
        Directory.CreateDirectory(targetDirectory);

        var extension = Path.GetExtension(sourceFilePath);
        var baseTargetName = BuildStoredFileName(request.DeptRecordId, safeDisplayName, extension);
        var targetPath = ResolveUniqueTargetPath(targetDirectory, baseTargetName, extension);

        try
        {
            File.Copy(sourceFilePath, targetPath, overwrite: false);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("Attachment copy failed. Please retry with a different file name or verify folder access.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException("Attachment copy failed due to file permissions.", ex);
        }

        var normalized = request with
        {
            DeptName = NormalizeDeptName(request.DeptName),
            DisplayName = safeDisplayName,
            UserName = NormalizeUser(request.UserName)
        };

        var result = _repository.AddAttachmentMetadata(normalized, targetPath, normalized.UserName);
        _auditLogService.BestEffortLog(
            actionType: "attachment.add",
            entityType: "Attachment",
            entityKey: $"{AuditLogService.BuildSessionKey(normalized.SessionId)}|dept:{normalized.DeptName}",
            userName: normalized.UserName,
            details: new { sessionId = normalized.SessionId, deptRecordId = normalized.DeptRecordId, normalized.DeptName, normalized.DisplayName });

        return result;
    }

    public AttachmentListResult RemoveAttachment(AttachmentRemoveRequest request)
    {
        if (request.AttachmentId <= 0)
        {
            throw new InvalidOperationException("attachmentId is required.");
        }

        // Stage 2D: metadata soft-delete only; physical cleanup remains deferred.
        var result = _repository.RemoveAttachment(request.AttachmentId);
        _auditLogService.BestEffortLog(
            actionType: "attachment.remove",
            entityType: "Attachment",
            entityKey: $"{AuditLogService.BuildSessionKey(result.SessionId)}|dept:{result.DeptName}",
            userName: NormalizeUser(request.UserName),
            details: new { attachmentId = request.AttachmentId, sessionId = result.SessionId, result.DeptRecordId, result.DeptName });

        return result;
    }

    public AttachmentViewerPayload GetViewerPayload(AttachmentViewerRequest request)
    {
        ValidateSessionAndDept(request.SessionId, request.DeptRecordId, "viewer");
        if (request.AttachmentId <= 0)
        {
            throw new InvalidOperationException("attachmentId is required for viewer payload.");
        }

        var payload = _repository.GetViewerPayload(request.SessionId, request.DeptRecordId, request.AttachmentId);

        var current = ValidateViewerAttachmentPath(payload.Current);
        var previous = payload.Previous is null ? null : ValidateViewerAttachmentPath(payload.Previous);
        var next = payload.Next is null ? null : ValidateViewerAttachmentPath(payload.Next);

        return payload with
        {
            Current = current,
            Previous = previous,
            Next = next
        };
    }

    private AttachmentPayload ValidateViewerAttachmentPath(AttachmentPayload attachment)
    {
        var normalizedPath = NormalizeAndValidateManagedPath(attachment.FilePath);
        return attachment with { FilePath = normalizedPath };
    }

    private string NormalizeAndValidateManagedPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException("Attachment file path is missing.");
        }

        var fullPath = Path.GetFullPath(path);
        if (!fullPath.StartsWith(_attachmentsRootFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Attachment file path is outside managed attachment storage.");
        }

        return fullPath;
    }

    private static string ResolveUniqueTargetPath(string targetDirectory, string baseTargetName, string extension)
    {
        var baseFileName = Path.GetFileNameWithoutExtension(baseTargetName);
        var candidate = Path.Combine(targetDirectory, baseTargetName);

        var attempt = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(targetDirectory, $"{baseFileName}_{attempt}{extension}");
            attempt += 1;
        }

        return candidate;
    }

    private static void ValidateSessionAndDept(long sessionId, long deptRecordId, string deptName)
    {
        if (sessionId <= 0)
        {
            throw new InvalidOperationException("sessionId is required.");
        }

        if (deptRecordId <= 0)
        {
            throw new InvalidOperationException("deptRecordId is required.");
        }

        if (string.IsNullOrWhiteSpace(deptName))
        {
            throw new InvalidOperationException("deptName is required.");
        }
    }

    private static string NormalizeDeptName(string deptName)
    {
        var normalized = (deptName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new InvalidOperationException("deptName is required.");
        }

        return normalized;
    }

    private static string NormalizeDisplayName(string displayName, string sourceFilePath)
    {
        var candidate = string.IsNullOrWhiteSpace(displayName)
            ? Path.GetFileName(sourceFilePath)
            : displayName.Trim();

        if (candidate.Length > 255)
        {
            return candidate[..255];
        }

        return candidate;
    }

    private static string NormalizeUser(string userName)
    {
        if (!string.IsNullOrWhiteSpace(userName))
        {
            return userName.Trim();
        }

        return Environment.UserName;
    }

    private static string BuildStoredFileName(long deptRecordId, string displayName, string extension)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff", CultureInfo.InvariantCulture);
        var baseName = Path.GetFileNameWithoutExtension(displayName);
        var safeBase = ToSafePathSegment(baseName);
        if (string.IsNullOrWhiteSpace(safeBase))
        {
            safeBase = "attachment";
        }

        return $"d{deptRecordId}_{stamp}_{safeBase}{extension}";
    }

    private static string ToSafePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
        }

        return sb.ToString().Trim().Replace(' ', '_');
    }

    private static string EnsureTrailingDirectorySeparator(string path)
    {
        if (path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path;
        }

        return path + Path.DirectorySeparatorChar;
    }

    private static string ResolveShiftDateFolder(long sessionId)
    {
        return $"session_{sessionId}";
    }
}
