using CarinaStudio.Threading;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Dialog to let user select the directory to import application data.
/// </summary>
public class ApplicationDataImportDirectorySelectionDialog : CommonDialog<string?>
{
    // Fields.
    bool showConfirmation = true;


    /// <summary>
    /// Get or set whether confirmation should be shown or not before selecting the directory.
    /// </summary>
    public bool ShowConfirmation
    {
        get => this.showConfirmation;
        set
        {
            this.VerifyAccess();
            this.VerifyShowing();
            this.showConfirmation = value;
        }
    }
    
    
    /// <inheritdoc/>
    protected override async Task<string?> ShowDialogCore(Avalonia.Controls.Window? owner)
    {
        // check state
        if (owner is null || IAppSuiteApplication.CurrentOrNull is not AppSuiteApplication app)
            return null;
        
        // confirm
        if (this.showConfirmation)
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
            var dirList = await owner.StorageProvider.OpenFolderPickerAsync(new()
            {
                AllowMultiple = false,
                Title = app.GetString("ApplicationDataImportDirectorySelectionDialog.Title")
            });
            if (dirList.Count != 1)
                return null;
            var directoryUri = dirList[0].Path;
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
        var validationStartTimestamp = Stopwatch.GetTimestamp();
        var processingDialog = new ProcessingDialog
        {
            Message = app.GetObservableString("Common.Validating")
        };
        _ = processingDialog.ShowDialog(owner);
        var isValidDirectory = await app.ValidateApplicationDataImportAsync(directory, CancellationToken.None);
        var validationDelay = 1000 - (long)Stopwatch.GetElapsedTime(validationStartTimestamp).TotalMilliseconds;
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