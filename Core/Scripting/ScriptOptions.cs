using System;
using System.Collections.Generic;
using System.Reflection;

namespace CarinaStudio.AppSuite.Scripting;

/// <summary>
/// Options of script.
/// </summary>
public struct ScriptOptions
{
    /// <summary>
    /// Get or set type of context.
    /// </summary>
    public Type? ContextType { get; set; }


    /// <summary>
    /// Get or set types which provide extension methods.
    /// </summary>
    public ISet<Type>? ExtensionTypes { get; set; }


    /// <summary>
    /// Get or set default imported namespaces.
    /// </summary>
    public ISet<string>? ImportedNamespaces { get; set; }


    /// <summary>
    /// Get or set default imported types.
    /// </summary>
    public ISet<Type>? ImportedTypes { get; set; }


    /// <summary>
    /// Get or set assemblies referenced by script.
    /// </summary>
    public ISet<Assembly>? ReferencedAssemblies { get; set; }
}