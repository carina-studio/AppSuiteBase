using CarinaStudio.MacOS.ObjectiveC;
using System;

namespace CarinaStudio.AppSuite.Native;

/// <summary>
/// A panel that prompts the user to select a file to open.
/// </summary>
class NSOpenPanel : NSObject
{
    // Static fields.
    static Property? AllowedContentTypesProperty;
    static Property? AllowsMultipleSelectionProperty;
    static Property? CanChooseDirectoriesProperty;
    static Property? CanChooseFilesProperty;
    static Property? CanDownloadUbiquitousContentsProperty;
    static readonly Class? NSOpenPanelClass = Platform.IsMacOS
        ? Class.GetClass("NSOpenPanel").AsNonNull()
        : null;
    static Selector? OpenPanelSelector;
    static Selector? RunModalSelector;
    static Property? TreatsFilePackagesAsDirectoriesProperty;
    static Property? UrlsProperty;
    
    
    // Constructor.
#pragma warning disable IDE0051
    NSOpenPanel(IntPtr handle, bool ownsInstance) : base(handle, ownsInstance) =>
        this.VerifyClass(NSOpenPanelClass!);
#pragma warning restore IDE0051
    NSOpenPanel(Class cls, IntPtr handle, bool ownsInstance) : base(cls, handle, ownsInstance)
    { }


    /// <summary>
    /// An array of types that specify the files types to which you can open.
    /// </summary>
    public NSArray<UTType>? AllowedContentTypes
    {
        get
        {
            AllowedContentTypesProperty ??= NSOpenPanelClass!.GetProperty("allowedContentTypes").AsNonNull();
            return this.GetProperty<NSArray<UTType>>(AllowedContentTypesProperty);
        }
        set
        {
            AllowedContentTypesProperty ??= NSOpenPanelClass!.GetProperty("allowedContentTypes").AsNonNull();
            this.SetProperty(AllowedContentTypesProperty, value);
        }
    }
    
    
    /// <summary>
    /// A Boolean that indicates whether the user may select multiple files and directories.
    /// </summary>
    public bool AllowsMultipleSelection
    {
        get
        {
            AllowsMultipleSelectionProperty ??= NSOpenPanelClass!.GetProperty("allowsMultipleSelection").AsNonNull();
            return this.GetProperty<bool>(AllowsMultipleSelectionProperty);
        }
        set
        {
            AllowsMultipleSelectionProperty ??= NSOpenPanelClass!.GetProperty("allowsMultipleSelection").AsNonNull();
            this.SetProperty(AllowsMultipleSelectionProperty, value);
        }
    }
    
    
    /// <summary>
    /// A Boolean that indicates whether the user can choose directories in the panel.
    /// </summary>
    public bool CanChooseDirectories
    {
        get
        {
            CanChooseDirectoriesProperty ??= NSOpenPanelClass!.GetProperty("canChooseDirectories").AsNonNull();
            return this.GetProperty<bool>(CanChooseDirectoriesProperty);
        }
        set
        {
            CanChooseDirectoriesProperty ??= NSOpenPanelClass!.GetProperty("canChooseDirectories").AsNonNull();
            this.SetProperty(CanChooseDirectoriesProperty, value);
        }
    }


    /// <summary>
    /// A Boolean that indicates whether the user can choose files in the panel.
    /// </summary>
    public bool CanChooseFiles
    {
        get
        {
            CanChooseFilesProperty ??= NSOpenPanelClass!.GetProperty("canChooseFiles").AsNonNull();
            return this.GetProperty<bool>(CanChooseFilesProperty);
        }
        set
        {
            CanChooseFilesProperty ??= NSOpenPanelClass!.GetProperty("canChooseFiles").AsNonNull();
            this.SetProperty(CanChooseFilesProperty, value);
        }
    }
    
    
    /// <summary>
    /// A Boolean value that indicates how the panel responds to iCloud documents that aren’t fully downloaded locally.
    /// </summary>
    public bool CanDownloadUbiquitousContents
    {
        get
        {
            CanDownloadUbiquitousContentsProperty ??= NSOpenPanelClass!.GetProperty("canDownloadUbiquitousContents").AsNonNull();
            return this.GetProperty<bool>(CanDownloadUbiquitousContentsProperty);
        }
        set
        {
            CanDownloadUbiquitousContentsProperty ??= NSOpenPanelClass!.GetProperty("canDownloadUbiquitousContents").AsNonNull();
            this.SetProperty(CanDownloadUbiquitousContentsProperty, value);
        }
    }


    /// <summary>
    /// Get a <see cref="NSOpenPanel"/> instance.
    /// </summary>
    /// <returns><see cref="NSOpenPanel"/>.</returns>
    public static NSOpenPanel OpenPanel()
    {
        OpenPanelSelector ??= Selector.FromName("openPanel");
        return SendMessage<NSOpenPanel>(NSOpenPanelClass!.Handle, OpenPanelSelector);
    }


    public int RunModal()
    {
        RunModalSelector ??= Selector.FromName("runModal");
        return this.SendMessage<int>(RunModalSelector);
    }
    
    
    /// <summary>
    /// A Boolean that indicates whether to treat file packages as directories.
    /// </summary>
    public bool TreatsFilePackagesAsDirectories
    {
        get
        {
            TreatsFilePackagesAsDirectoriesProperty ??= NSOpenPanelClass!.GetProperty("treatsFilePackagesAsDirectories").AsNonNull();
            return this.GetProperty<bool>(TreatsFilePackagesAsDirectoriesProperty);
        }
        set
        {
            TreatsFilePackagesAsDirectoriesProperty ??= NSOpenPanelClass!.GetProperty("treatsFilePackagesAsDirectories").AsNonNull();
            this.SetProperty(TreatsFilePackagesAsDirectoriesProperty, value);
        }
    }


    /// <summary>
    /// An array of URLs, each of which contains the fully specified location of a selected file or directory.
    /// </summary>
    public NSArray<NSURL> Urls
    {
        get
        {
            UrlsProperty ??= NSOpenPanelClass!.GetProperty("URLs").AsNonNull();
            return this.GetProperty<NSArray<NSURL>>(UrlsProperty);
        }
    }
}