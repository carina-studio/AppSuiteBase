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
    static FontFamily? notoSansSC;
    static FontFamily? notoSansTC;
    static FontFamily? roboto;
    static FontFamily? robotoMono;
    static FontFamily? sourceCodePro;


    /// <summary>
    /// Get all built-in font families.
    /// </summary>
    public static IList<FontFamily> FontFamilies =>
        // ReSharper disable once InvokeAsExtensionMethod
        fontFamilies ?? ListExtensions.AsReadOnly(new[]
        {
            IBMPlexMono,
            NotoSansSC,
            NotoSansTC,
            Roboto,
            RobotoMono,
            SourceCodePro,
        }).Also(it => fontFamilies = it);


    /// <summary>
    /// IBM Plex Mono.
    /// </summary>
    public static FontFamily IBMPlexMono => ibmPlexMono ?? new FontFamily(baseResourceUri, "#IBM Plex Mono").Also(it => ibmPlexMono = it);
    
    
    /// <summary>
    /// Noto Sans Simplified Chinese.
    /// </summary>
    public static FontFamily NotoSansSC => notoSansSC ?? new FontFamily(new($"avares://CarinaStudio.AppSuite.Core/Fonts/"), "#Noto Sans SC").Also(it => notoSansSC = it);
    
    
    /// <summary>
    /// Noto Sans Traditional Chinese.
    /// </summary>
    public static FontFamily NotoSansTC => notoSansTC ?? new FontFamily(new($"avares://CarinaStudio.AppSuite.Core/Fonts/"), "#Noto Sans TC").Also(it => notoSansTC = it);
    

    /// <summary>
    /// Roboto.
    /// </summary>
    public static FontFamily Roboto => roboto ?? new FontFamily(baseResourceUri, "#Roboto").Also(it => roboto = it);


    /// <summary>
    /// Roboto Mono.
    /// </summary>
    public static FontFamily RobotoMono => robotoMono ?? new FontFamily(baseResourceUri, "#Roboto Mono").Also(it => robotoMono = it);


    /// <summary>
    /// Source Code Pro.
    /// </summary>
    public static FontFamily SourceCodePro => sourceCodePro ?? new FontFamily(baseResourceUri, "#Source Code Pro").Also(it => sourceCodePro = it);
}