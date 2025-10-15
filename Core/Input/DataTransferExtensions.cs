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
    const int GCHandleDataSignature = 0x47434864; // 'GCHd'
    
    
    /// <summary>
    /// Add value to the <see cref="DataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer"><see cref="DataTransfer"/>.</param>
    /// <param name="format">Format.</param>
    /// <param name="value">Value.</param>
    /// <typeparam name="T">Type of value.</typeparam>
    public static void Add<T>(this DataTransfer dataTransfer, DataFormat<T> format, T? value) where T : class =>
        dataTransfer.Add(DataTransferItem.Create(format, value));


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
            var data = GC.AllocateUninitializedArray<byte>(sizeof(int) + sizeof(nint));
            fixed (byte* pData = data)
            {
                *(int*)pData = GCHandleDataSignature;
                *(nint*)(pData + sizeof(int)) = GCHandle.ToIntPtr(handle);
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
        if (data is null || data.Length != sizeof(int) + sizeof(nint))
        {
            handle = default;
            return false;
        }
        fixed (byte* pData = data)
        {
            if (*(int*)pData != GCHandleDataSignature)
            {
                handle = default;
                return false;
            }
            handle = GCHandle.FromIntPtr(*(nint*)(pData + sizeof(int)));
        }
        return handle != default;
    }
}