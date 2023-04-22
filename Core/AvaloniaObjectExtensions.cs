using Avalonia;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Extensions for <see cref="AvaloniaObject"/>.
/// </summary>
public static class AvaloniaObjectExtensions
{
    // Fields.
    static FieldInfo? AvaloniaObjectDirectBindingsField;
    
    
    /// <summary>
    /// Clear all bindings.
    /// </summary>
    /// <param name="obj"><see cref="AvaloniaObject"/>.</param>
    public static void ClearBindings(this AvaloniaObject obj)
    {
        if (!SetupAvaloniaObjectReflection())
            return;
        (AvaloniaObjectDirectBindingsField.GetValue(obj) as IEnumerable<IDisposable>)?.Let(it =>
        {
            if (it is IList<IDisposable> list)
            {
                for (var i = list.Count - 1; i >= 0; --i)
                    list[i].Dispose();
            }
            else
            {
                foreach (var bindingToken in it.ToArray())
                    bindingToken.Dispose();
            }
        });
    }


    // Setup reflection for AvaloniaObject.
    [MemberNotNullWhen(true, nameof(AvaloniaObjectDirectBindingsField))]
    static bool SetupAvaloniaObjectReflection()
    {
        if (AvaloniaObjectDirectBindingsField is not null)
            return true;
        AvaloniaObjectDirectBindingsField = typeof(AvaloniaObject).GetField("_directBindings", BindingFlags.Instance | BindingFlags.NonPublic);
        return AvaloniaObjectDirectBindingsField is not null;
    }
}