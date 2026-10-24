using Avalonia.Controls;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Extensions for <see cref="TopLevel"/>.
/// </summary>
static class TopLevelExtensions
{
    // Static fields.
    static MethodInfo? ClearPointerOverMethod;
    static FieldInfo? PointerOverPreProcessorField;
    static PropertyInfo? PresentationSourceProperty;


    /// <summary>
    /// Clear pointer-over state of all elements in given <see cref="TopLevel"/>, the state will be updated again by next pointer event.
    /// </summary>
    /// <remarks>
    /// The last known position of pointer is also dropped so that the pointer-over state won't be updated by
    /// position which is no longer valid, for example the position at which a drag-and-drop operation was started.
    /// </remarks>
    /// <param name="topLevel"><see cref="TopLevel"/>.</param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties, typeof(TopLevel))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields, "Avalonia.Controls.PresentationSource", "Avalonia.Controls")]
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods, "Avalonia.Input.PointerOverPreProcessor", "Avalonia.Base")]
    [RequiresUnreferencedCode("Get internal state from Avalonia.")]
    public static void ClearPointerOver(this TopLevel topLevel)
    {
        // skip if pointer is not over the top level
        if (!topLevel.IsPointerOver)
            return;

        // get internal members, AsNonNull() is intentional: a NRE here on Avalonia upgrade is the signal that
        // the TopLevel.PresentationSource -> _pointerOverPreProcessor -> ClearPointerOver() chain moved and the workaround needs revisiting
        if (PresentationSourceProperty is null)
        {
            var presentationSourceType = typeof(TopLevel).Assembly.GetType("Avalonia.Controls.PresentationSource").AsNonNull();
            PresentationSourceProperty = typeof(TopLevel).GetProperty("PresentationSource", BindingFlags.Instance | BindingFlags.NonPublic, null, presentationSourceType, Type.EmptyTypes, null).AsNonNull();
            PointerOverPreProcessorField = presentationSourceType.GetField("_pointerOverPreProcessor", BindingFlags.Instance | BindingFlags.NonPublic).AsNonNull();
        }
        var pointerOverPreProcessor = PointerOverPreProcessorField.AsNonNull().GetValue(PresentationSourceProperty.GetValue(topLevel).AsNonNull()).AsNonNull();
        ClearPointerOverMethod ??= pointerOverPreProcessor.GetType().GetMethod("ClearPointerOver", BindingFlags.Instance | BindingFlags.NonPublic, [ ]).AsNonNull();

        // clear pointer-over state
        ClearPointerOverMethod.Invoke(pointerOverPreProcessor, [ ]);
    }
}