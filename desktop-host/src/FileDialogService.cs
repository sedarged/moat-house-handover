using System.IO;
using Microsoft.Win32;

namespace MoatHouseHandover.Host;

public sealed class FileDialogService
{
    public FilePickResult PickFile(FilePickRequest request)
    {
        var dialog = new OpenFileDialog
        {
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Select attachment" : request.Title,
            Filter = string.IsNullOrWhiteSpace(request.Filter)
                ? "Supported files|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.tif;*.tiff|All files|*.*"
                : request.Filter,
            CheckFileExists = true,
            Multiselect = false
        };

        var result = dialog.ShowDialog();
        if (result != true || string.IsNullOrWhiteSpace(dialog.FileName))
        {
            return new FilePickResult(false, null, null);
        }

        return new FilePickResult(true, dialog.FileName, Path.GetFileName(dialog.FileName));
    }
}
