using CarinaStudio.MacOS.ObjectiveC;
using System;

namespace CarinaStudio.AppSuite.Native;

/// <summary>
/// A structure that represents a type of data to load, send, or receive.
/// </summary>
class UTType : NSObject
{
    // Static fields.
    static Selector? InitWithIdentifierSelector;
    static readonly Class? UTTypeClass = Platform.IsMacOS
        ? Class.GetClass("UTType").AsNonNull()
        : null;
    
    
    // Constructor.
#pragma warning disable IDE0051
    UTType(IntPtr handle, bool ownsInstance) : base(handle, ownsInstance) =>
        this.VerifyClass(UTTypeClass!);
#pragma warning restore IDE0051
    UTType(Class cls, IntPtr handle, bool ownsInstance) : base(cls, handle, ownsInstance)
    { }


    /// <summary>
    /// Create a type based on an identifier.
    /// </summary>
    /// <param name="identifier">The identifier of your type.</param>
    /// <returns><see cref="UTType"/> with given identifier, or Null if the system doesn’t know the type identifier.</returns>
    public static UTType? WithIdentifier(string identifier)
    {
        InitWithIdentifierSelector ??= Selector.FromName("typeWithIdentifier:");
        using var identifierString = new NSString(identifier);
        var handle = SendMessage<IntPtr>(UTTypeClass!.Handle, InitWithIdentifierSelector, identifierString);
        return handle != IntPtr.Zero ? FromHandle<UTType>(handle, ownsInstance: true) : null;
    }
}