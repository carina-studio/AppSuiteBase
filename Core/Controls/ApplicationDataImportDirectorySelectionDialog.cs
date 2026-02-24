using CarinaStudio.AppSuite.Native;
using CarinaStudio.MacOS.ObjectiveC;
using System;
using System.Diagnostics;
using System.Threading;
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
        if (owner is null || IAppSuiteApplication.CurrentOrNull is not AppSuiteApplication app)
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
        string? directory;
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
            {
                directory = directoryUri.LocalPath;
                break;
            }
            await new MessageDialog
            {
                Icon = MessageDialogIcon.Warning,
                Message = app.GetObservableString("ApplicationDataImportDirectorySelectionDialog.LocalDirectoriesOnly"),
                Title = app.GetObservableString("ApplicationDataImportDirectorySelectionDialog.Title")
            }.ShowDialog(owner);
            if (!owner.IsVisible)
                return null;
        }
        
        // validate directory
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var processingDialog = new ProcessingDialog
        {
            Message = app.GetObservableString("Common.Validating")
        };
        _ = processingDialog.ShowDialog(owner);
        var isValidDirectory = await app.ValidateApplicationDataImportAsync(directory, CancellationToken.None);
        var validationDelay = 1000 - stopWatch.ElapsedMilliseconds;
        if (validationDelay > 0)
            await Task.Delay((int)validationDelay);
        processingDialog.Complete();
        if (!owner.IsVisible)
            return null;
        if (!isValidDirectory)
        {
            var confirmationResult = await new MessageDialog
            {
                Buttons = MessageDialogButtons.YesNo,
                DefaultResult = MessageDialogResult.No,
                Icon = MessageDialogIcon.Warning,
                Message = new FormattedString().Also(it =>
                {
                    it.Arg1 = directory;
                    it.Bind(FormattedString.FormatProperty, app.GetObservableString("ApplicationDataImportDirectorySelectionDialog.DirectoryValidationFailed"));
                }),
                Title = app.GetObservableString("ApplicationDataImportDirectorySelectionDialog.Title")
            }.ShowDialog(owner);
            if (confirmationResult != MessageDialogResult.Yes)
                return null;
        }
        return directory;
    }
}