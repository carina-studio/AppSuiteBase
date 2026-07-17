using Avalonia.Input;
using System;
using System.Runtime.InteropServices;

namespace CarinaStudio.AppSuite.Input;

/// <summary>
/// Extension methods for <see cref="IDataTransfer"/> and <see cref="DataTransfer"/>.
/// </summary>
public static class DataTransferExtensions
{
    // Constants.
    const int GCHandleDataName = 0x47434864; // 'GCHd'
    static readonly int GCHandleDataKey = new Random().Next();
    
    
    // GCHandle data placed in DataTransfer.
    [StructLayout(LayoutKind.Sequential)]
    ref struct GCHandleData
    {
        public int Name;
        public int Key;
        public IntPtr Handle;
    }


    /// <summary>
    /// Add <see cref="GCHandle"/> to the <see cref="DataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer"><see cref="DataTransfer"/>.</param>
    /// <param name="format">Format.</param>
    /// <param name="handle"><see cref="GCHandle"/>.</param>
    public static unsafe void Add(this DataTransfer dataTransfer, DataFormat<byte[]> format, GCHandle handle)
    {
        if (handle != default)
        {
            var data = GC.AllocateUninitializedArray<byte>(sizeof(GCHandleData));
            fixed (byte* p = data)
            {
                var pData = (GCHandleData*)p;
                pData->Name = GCHandleDataName;
                pData->Key = GCHandleDataKey;
                pData->Handle = GCHandle.ToIntPtr(handle);
            }
            dataTransfer.Add(DataTransferItem.Create(format, data));
        }
        else
            dataTransfer.Add(DataTransferItem.Create(format, (byte[]?)null));
    }


    /// <summary>
    /// Try getting <see cref="GCHandle"/> from <see cref="IDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer"><see cref="IDataTransfer"/>.</param>
    /// <param name="format">Format.</param>
    /// <param name="handle"><see cref="GCHandle"/> got from <see cref="IDataTransfer"/>.</param>
    /// <returns>True if <see cref="GCHandle"/> was successfully got from <see cref="IDataTransfer"/>.</returns>
    public static unsafe bool TryGetGCHandle(this IDataTransfer dataTransfer, DataFormat<byte[]> format, out GCHandle handle)
    {
        var data = dataTransfer.TryGetValue(format);
        if (data is null || data.Length != sizeof(GCHandleData))
        {
            handle = default;
            return false;
        }
        fixed (byte* p = data)
        {
            var pData = (GCHandleData*)p;
            if (pData->Name != GCHandleDataName || pData->Key != GCHandleDataKey)
            {
                handle = default;
                return false;
            }
            handle = GCHandle.FromIntPtr(pData->Handle);
        }
        return handle != default;
    }
}