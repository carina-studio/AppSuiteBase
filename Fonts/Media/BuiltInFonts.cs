using Avalonia.Media;
using CarinaStudio.Collections;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Built-in fonts.
/// </summary>
public static class BuiltInFonts
{
    // Fields.
    static readonly Uri baseResourceUri = new($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}/Resources/Fonts/");
    static IList<FontFamily>? fontFamilies;
    static FontFamily? ibmPlexMono;
    static FontFamily? roboto;
    static FontFamily? robotoMono;
    static FontFamily? sourceCodePro;


    /// <summary>
    /// Get all built-in font families.
    /// </summary>
    public static IList<FontFamily> FontFamilies { get => fontFamilies ?? ListExtensions.AsReadOnly(new FontFamily[]
    {
        IBMPlexMono,
        Roboto,
        RobotoMono,
        SourceCodePro,
    }).Also(it => fontFamilies = it); }


    /// <summary>
    /// IBM Plex Mono.
    /// </summary>
    public static FontFamily IBMPlexMono { get => ibmPlexMono ?? new FontFamily(baseResourceUri, "#IBM Plex Mono").Also(it => ibmPlexMono = it); }


    /// <summary>
    /// Roboto.
    /// </summary>
    public static FontFamily Roboto { get => roboto ?? new FontFamily(baseResourceUri, "#Roboto").Also(it => roboto = it); }


    /// <summary>
    /// Roboto Mono.
    /// </summary>
    public static FontFamily RobotoMono { get => robotoMono ?? new FontFamily(baseResourceUri, "#Roboto Mono").Also(it => robotoMono = it); }


    /// <summary>
    /// Source Code Pro.
    /// </summary>
    public static FontFamily SourceCodePro { get => sourceCodePro ?? new FontFamily(baseResourceUri, "#Source Code Pro").Also(it => sourceCodePro = it); }
}