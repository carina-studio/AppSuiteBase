using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CarinaStudio.AppSuite.Diagnostics;

/// <summary>
/// Provide guarding functions.
/// </summary>
public static class Guard
{
    /// <summary>
    /// Throw <see cref="InvalidOperationException"/> if the caller is not one of Carina Studio assemblies.
    /// </summary>
    [RequiresUnreferencedCode("Unreferenced code is required for checking calling assembly.")]
    public static void VerifyInternalCall()
    {
        var appAssembly = Application.CurrentOrNull?.Assembly;
        var stackTrace = new StackTrace();
        var frameCount = stackTrace.FrameCount;
        for (var i = 2; i < frameCount; ++i)
        {
            var assembly = stackTrace.GetFrame(i)?.GetMethod()?.DeclaringType?.Assembly;
            if (assembly is null)
                continue;
            if (assembly == appAssembly)
                return;
            var assemblyName = assembly.FullName;
            if (assemblyName == null)
                continue;
            if (assemblyName.StartsWith(value: "CarinaStudio."))
                return;
            if (assemblyName.StartsWith("System."))
                continue;
            throw new InvalidOperationException();
        }
    }
}