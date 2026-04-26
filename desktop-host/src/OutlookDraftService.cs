using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace MoatHouseHandover.Host;

public sealed class OutlookDraftService
{
    public OutlookDraftResult CreateDraft(OutlookDraftRequest request)
    {
        if (!OperatingSystem.IsWindows())
        {
            return Failure("Outlook draft creation requires Windows desktop with Outlook installed.");
        }

        Type? outlookType = null;
        object? outlookApp = null;
        object? mailItem = null;

        try
        {
            outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType is null)
            {
                return Failure("Outlook COM runtime is unavailable. Install Outlook desktop to create drafts.");
            }

            outlookApp = Activator.CreateInstance(outlookType);
            if (outlookApp is null)
            {
                return Failure("Outlook application instance could not be created.");
            }

            dynamic app = outlookApp;
            mailItem = app.CreateItem(0);
            dynamic draft = mailItem;

            draft.To = request.ToList ?? string.Empty;
            draft.CC = request.CcList ?? string.Empty;
            draft.Subject = request.Subject ?? string.Empty;
            draft.Body = request.Body ?? string.Empty;

            var attachedCount = 0;
            foreach (var path in request.AttachmentPaths)
            {
                if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
                {
                    continue;
                }

                draft.Attachments.Add(path);
                attachedCount++;
            }

            draft.Save();
            string? entryId = null;
            try
            {
                entryId = draft.EntryID;
            }
            catch
            {
                entryId = null;
            }

            return new OutlookDraftResult(
                DraftCreated: true,
                Message: "Outlook draft created. Email was not sent.",
                DraftEntryId: entryId,
                CreatedAt: DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                AttachmentCount: attachedCount);
        }
        catch (Exception ex)
        {
            return Failure($"Outlook draft creation failed: {ex.Message}");
        }
        finally
        {
            ReleaseCom(mailItem);
            ReleaseCom(outlookApp);
        }
    }

    private static OutlookDraftResult Failure(string message)
    {
        return new OutlookDraftResult(
            DraftCreated: false,
            Message: message,
            DraftEntryId: null,
            CreatedAt: DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            AttachmentCount: 0);
    }

    private static void ReleaseCom(object? value)
    {
        if (value is null || !Marshal.IsComObject(value))
        {
            return;
        }

        Marshal.FinalReleaseComObject(value);
    }
}
