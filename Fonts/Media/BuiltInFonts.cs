using Avalonia.Media;
using CarinaStudio.Collections;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Built-in fonts.
/// </summary>
public static class BuiltInFonts
{
    // Fields.
    static IList<FontFamily>? fontFamilies;
    static FontFamily? robotoMono;
    static FontFamily? sourceCodePro;


    /// <summary>
    /// Get all built-in font families.
    /// </summary>
    public static IList<FontFamily> FontFamilies { get => fontFamilies ?? throw new InvalidOperationException(); }


    /// <summary>
    /// Initialize asynchronously.
    /// </summary>
    /// <returns>Task of iitialization.</returns>
    public static Task InitializeAsync()
    {
        var baseUri = new Uri($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}/Resources/Fonts/");
        robotoMono = new(baseUri, "#Roboto Mono");
        sourceCodePro = new(baseUri, "#Source Code Pro");
        fontFamilies = ListExtensions.AsReadOnly(new FontFamily[]
        {
            robotoMono,
            sourceCodePro,
        });
        return Task.CompletedTask;
    }


    /// <summary>
    /// Roboto Mono.
    /// </summary>
    public static FontFamily RobotoMono { get => robotoMono ?? throw new InvalidOperationException(); }


    /// <summary>
    /// Source Code Pro.
    /// </summary>
    public static FontFamily SourceCodePro { get => sourceCodePro ?? throw new InvalidOperationException(); }
}