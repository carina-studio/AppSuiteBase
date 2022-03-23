using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Group box.
    /// </summary>
    public class GroupBox : HeaderedContentControl, IStyleable
    {
        // Style key.
        Type IStyleable.StyleKey { get; } = typeof(GroupBox);
    }
}