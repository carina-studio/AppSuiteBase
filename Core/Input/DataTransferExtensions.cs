using Avalonia.Input;
using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite.Input;

/// <summary>
/// Extension methods for <see cref="IDataTransfer"/> and <see cref="DataTransfer"/>.
/// </summary>
public static class DataTransferExtensions
{
    // Constants.
    const int GCHandleDataName = 0x47434864; // 'GCHd'
    const int GCHandleDataSize = 16; // Name (Int32), Key (Int32) and Handle (Int64).
    
    
    // Static fields.
    static readonly int GCHandleDataKey = new Random().Next();


    /// <summary>
    /// Add <see cref="GCHandle"/> to the <see cref="DataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer"><see cref="DataTransfer"/>.</param>
    /// <param name="format">Format.</param>
    /// <param name="handle"><see cref="GCHandle"/>.</param>
    public static void Add(this DataTransfer dataTransfer, DataFormat<byte[]> format, GCHandle handle)
    {
        // add empty data if the handle is invalid
        if (handle == default)
        {
            dataTransfer.Add(DataTransferItem.Create(format, (byte[]?)null));
            return;
        }
        
        // add data which carries the handle
        var data = GC.AllocateUninitializedArray<byte>(GCHandleDataSize);
        BinaryPrimitives.WriteInt32LittleEndian(data, GCHandleDataName);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(4), GCHandleDataKey);
        BinaryPrimitives.WriteInt64LittleEndian(data.AsSpan(8), GCHandle.ToIntPtr(handle));
        dataTransfer.Add(DataTransferItem.Create(format, data));
    }


    /// <summary>
    /// Try getting <see cref="GCHandle"/> from <see cref="IDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer"><see cref="IDataTransfer"/>.</param>
    /// <param name="format">Format.</param>
    /// <param name="handle"><see cref="GCHandle"/> got from <see cref="IDataTransfer"/>.</param>
    /// <returns>True if <see cref="GCHandle"/> was successfully got from <see cref="IDataTransfer"/>.</returns>
    public static bool TryGetGCHandle(this IDataTransfer dataTransfer, DataFormat<byte[]> format, out GCHandle handle)
    {
        // get and verify data
        var data = dataTransfer.TryGetValue(format);
        if (data is null 
            || data.Length != GCHandleDataSize
            || BinaryPrimitives.ReadInt32LittleEndian(data) != GCHandleDataName
            || BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(4)) != GCHandleDataKey)
        {
            handle = default;
            return false;
        }
        
        // get the handle carried by the data
        handle = GCHandle.FromIntPtr((IntPtr)BinaryPrimitives.ReadInt64LittleEndian(data.AsSpan(8)));
        return handle != default;
    }
}