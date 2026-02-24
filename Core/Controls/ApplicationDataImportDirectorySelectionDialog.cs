using CarinaStudio.AppSuite.Native;
using CarinaStudio.MacOS.ObjectiveC;
using System;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to let user select the directory to import application data.
/// </summary>
public class ApplicationDataImportDirectorySelectionDialog : CommonDialog<string?>
{
    /// <inheritdoc/>
    protected override async Task<string?> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        // check state
        if (owner is null || IAppSuiteApplication.CurrentOrNull is not { } app)
            return null;
        
        // confirm
        if (!app.IsFirstLaunch)
        {
            var confirmationResult = await new MessageDialog
            {
                Buttons = MessageDialogButtons.OKCancel,
                DefaultResult = MessageDialogResult.Cancel,
                Icon = MessageDialogIcon.Warning,
                Message = app.GetObservableString("ApplicationDataImportDirectorySelectionDialog.Confirmation"),
                Title = app.GetObservableString("ApplicationDataImportDirectorySelectionDialog.Title")
            }.ShowDialog(owner);
            if (confirmationResult != MessageDialogResult.OK || !owner.IsVisible)
                return null;
        }
        
        // select directory
        while (true)
        {
            Uri directoryUri;
            if (Platform.IsNotMacOS)
            {
                var dirList = await owner.StorageProvider.OpenFolderPickerAsync(new()
                {
                    AllowMultiple = false,
                    Title = app.GetString("ApplicationDataImportDirectorySelectionDialog.Title")
                });
                if (dirList.Count != 1)
                    return null;
                directoryUri = dirList[0].Path;
            }
            else
            {
                using var openPanel = NSOpenPanel.OpenPanel().Also(it =>
                {
                    UTType.WithIdentifier("com.apple.application-bundle")?.Use(fileType =>
                    {
                        using var fileTypeArray = new NSArray<UTType>(fileType);
                        it.AllowedContentTypes = fileTypeArray;
                    });
                    it.AllowsMultipleSelection = false;
                    it.CanChooseDirectories = false;
                    it.CanChooseFiles = true;
                    it.CanDownloadUbiquitousContents = false;
                    it.TreatsFilePackagesAsDirectories = false;
                });
                var modelResponse = openPanel.RunModal();
                if (modelResponse != 1)
                    return null;
                var urls = openPanel.Urls;
                if (urls.Count != 1 || urls[0].AbsoluteString is not { } url)
                    return null;
                directoryUri = new Uri(url);
            }
            if (!owner.IsVisible)
                return null;
            if (directoryUri.Scheme == "file")
                return directoryUri.LocalPath;
            await new MessageDialog
            {
                Icon = MessageDialogIcon.Warning,
                Message = app.GetObservableString("ApplicationDataImportDirectorySelectionDialog.LocalDirectoriesOnly"),
                Title = app.GetObservableString("ApplicationDataImportDirectorySelectionDialog.Title")
            }.ShowDialog(owner);
            if (!owner.IsVisible)
                return null;
        }
    }
}