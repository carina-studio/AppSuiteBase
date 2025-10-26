using Avalonia.Input;
using Avalonia.Platform.Storage;
using CarinaStudio.Collections;
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
    /// Check whether the <see cref="IDataTransfer"/> contains one or more file items or not.
    /// </summary>
    /// <param name="dataTransfer"><see cref="IDataTransfer"/>.</param>
    /// <returns>True if the <see cref="IDataTransfer"/> contains one or more file items.</returns>
    public static bool HasFiles(this IDataTransfer dataTransfer) => dataTransfer.Contains(DataFormat.File);


    /// <summary>
    /// Try getting local path of files from <see cref="IDataTransfer"/>.
    /// </summary>
    /// <param name="dataTransfer"><see cref="IDataTransfer"/>.</param>
    /// <returns>Array of local path of files.</returns>
    public static string[]? TryGetLocalFilePaths(this IDataTransfer dataTransfer)
    {
        var files = dataTransfer.TryGetFiles();
        if (files.IsNullOrEmpty())
            return null;
        var fileCount = files.Length;
        var filePaths = GC.AllocateUninitializedArray<string>(files.Length);
        var filePathCount = 0;
        for (var i = 0; i < fileCount; ++i)
        {
            if (files[i].TryGetLocalPath() is { } filePath && filePath.Length > 0)
                filePaths[filePathCount++] = filePath;
        }
        if (filePathCount == fileCount)
            return filePaths;
        if (filePathCount == 0)
            return null;
        var subFilePaths = GC.AllocateUninitializedArray<string>(filePathCount);
        Array.Copy(filePaths, 0, subFilePaths, 0, filePathCount);
        return subFilePaths;
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