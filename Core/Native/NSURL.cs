using CarinaStudio.MacOS.ObjectiveC;
using System;

namespace CarinaStudio.AppSuite.Native;

/// <summary>
/// An object that represents the location of a resource, such as an item on a remote server or the path to a local file.
/// </summary>
class NSURL : NSObject
{
    // Static fields.
    static Property? AbsoluteStringProperty;
    static readonly Class? NSURLClass = Platform.IsMacOS
        ? Class.GetClass("NSURL").AsNonNull()
        : null;
    
    
    // Constructor.
#pragma warning disable IDE0051
    NSURL(IntPtr handle, bool ownsInstance) : base(handle, ownsInstance) =>
        this.VerifyClass(NSURLClass!);
#pragma warning restore IDE0051
    NSURL(Class cls, IntPtr handle, bool ownsInstance) : base(cls, handle, ownsInstance)
    { }


    /// <summary>
    /// The URL string for the receiver as an absolute URL. (read-only)
    /// </summary>
    public string? AbsoluteString
    {
        get
        {
            AbsoluteStringProperty ??= NSURLClass!.GetProperty("absoluteString").AsNonNull();
            return this.GetProperty<NSString?>(AbsoluteStringProperty)?.Use(it => it.ToString());
        }
    }
}