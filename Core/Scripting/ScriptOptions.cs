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
    /// Get or set types which provide extension methods.
    /// </summary>
    ISet<Type>? ExtensionTypes { get; set; }


    /// <summary>
    /// Get or set default imported namespaces.
    /// </summary>
    ISet<string>? ImportedNamespaces { get; set; }


    /// <summary>
    /// Get or set assemblies referenced by script.
    /// </summary>
    ISet<Assembly>? ReferencedAssemblies { get; set; }
}