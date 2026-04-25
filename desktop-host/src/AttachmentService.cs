using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace MoatHouseHandover.Host;

public sealed class AttachmentService
{
    private readonly AttachmentRepository _repository;
    private readonly HostConfig _config;

    public AttachmentService(AttachmentRepository repository, HostConfig config)
    {
        _repository = repository;
        _config = config;
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
            throw new InvalidOperationException("Source attachment file was not found.");
        }

        var safeDisplayName = NormalizeDisplayName(request.DisplayName, sourceFilePath);
        var shiftDatePath = ResolveShiftDateFolder(request.SessionId);
        var deptFolderName = ToSafePathSegment(request.DeptName);
        var targetDirectory = Path.Combine(_config.AttachmentsRoot, request.SessionId.ToString(CultureInfo.InvariantCulture), shiftDatePath, deptFolderName);
        Directory.CreateDirectory(targetDirectory);

        var extension = Path.GetExtension(sourceFilePath);
        var targetName = BuildStoredFileName(request.DeptRecordId, safeDisplayName, extension);
        var targetPath = Path.Combine(targetDirectory, targetName);
        File.Copy(sourceFilePath, targetPath, overwrite: false);

        var normalized = request with
        {
            DeptName = NormalizeDeptName(request.DeptName),
            DisplayName = safeDisplayName,
            UserName = NormalizeUser(request.UserName)
        };

        return _repository.AddAttachmentMetadata(normalized, targetPath, normalized.UserName);
    }

    public AttachmentListResult RemoveAttachment(AttachmentRemoveRequest request)
    {
        if (request.AttachmentId <= 0)
        {
            throw new InvalidOperationException("attachmentId is required.");
        }

        return _repository.RemoveAttachment(request.AttachmentId, NormalizeUser(request.UserName));
    }

    public AttachmentViewerPayload GetViewerPayload(AttachmentViewerRequest request)
    {
        ValidateSessionAndDept(request.SessionId, request.DeptRecordId, "viewer");
        if (request.AttachmentId <= 0)
        {
            throw new InvalidOperationException("attachmentId is required for viewer payload.");
        }

        return _repository.GetViewerPayload(request.SessionId, request.DeptRecordId, request.AttachmentId);
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

    private static string ResolveShiftDateFolder(long sessionId)
    {
        return $"session_{sessionId}";
    }
}
