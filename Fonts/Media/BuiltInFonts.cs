using Avalonia.Media;
using CarinaStudio.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Built-in fonts.
/// </summary>
public static class BuiltInFonts
{
    // Fields.
    static readonly Uri baseCoreInterResourceUri = new("avares://CarinaStudio.AppSuite.Core/Fonts/Inter");
    static readonly Uri baseCoreNotoResourceUri = new("avares://CarinaStudio.AppSuite.Core/Fonts/Noto");
    static readonly Uri baseResourceUri = new($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}/Resources/Fonts/");
    static IList<FontFamily>? fontFamilies;
    static FontFamily? ibmPlexMono;
    static FontFamily? inter;
    static FontFamily? notoSans;
    static FontFamily? notoSansMono;
    static FontFamily? notoSansSC;
    static FontFamily? notoSansTC;
    static FontFamily? notoSerif;
    static FontFamily? roboto;
    static FontFamily? robotoMono;
    static FontFamily? sourceCodePro;


    /// <summary>
    /// Get all built-in font families.
    /// </summary>
    public static IList<FontFamily> FontFamilies =>
        // ReSharper disable once InvokeAsExtensionMethod
        fontFamilies ?? ListExtensions.AsReadOnly(
        [
            IBMPlexMono,
            Inter,
            NotoSans,
            NotoSansMono,
            NotoSansSC,
            NotoSansTC,
            NotoSerif,
            Roboto,
            RobotoMono,
            SourceCodePro
        ]).Also(it => fontFamilies = it);


    /// <summary>
    /// IBM Plex Mono.
    /// </summary>
    public static FontFamily IBMPlexMono => ibmPlexMono ?? new FontFamily(baseResourceUri, "#IBM Plex Mono").Also(it => ibmPlexMono = it);
    
    
    /// <summary>
    /// Inter.
    /// </summary>
    public static FontFamily Inter => inter ?? new FontFamily(baseCoreInterResourceUri, "#Inter").Also(it => inter = it);
    
    
    /// <summary>
    /// Noto Sans.
    /// </summary>
    public static FontFamily NotoSans => notoSans ?? new FontFamily(baseCoreNotoResourceUri, "#Noto Sans").Also(it => notoSans = it);
    
    
    /// <summary>
    /// Noto Sans Mono.
    /// </summary>
    public static FontFamily NotoSansMono => notoSansMono ?? new FontFamily(baseCoreNotoResourceUri, "#Noto Sans Mono").Also(it => notoSansMono = it);
    
    
    /// <summary>
    /// Noto Sans Simplified Chinese.
    /// </summary>
    public static FontFamily NotoSansSC => notoSansSC ?? new FontFamily(baseCoreNotoResourceUri, "#Noto Sans SC").Also(it => notoSansSC = it);
    
    
    /// <summary>
    /// Noto Sans Traditional Chinese.
    /// </summary>
    public static FontFamily NotoSansTC => notoSansTC ?? new FontFamily(baseCoreNotoResourceUri, "#Noto Sans TC").Also(it => notoSansTC = it);
    
    
    /// <summary>
    /// Noto Serif.
    /// </summary>
    public static FontFamily NotoSerif => notoSerif ?? new FontFamily(baseCoreNotoResourceUri, "#Noto Serif").Also(it => notoSerif = it);


    /// <summary>
    /// Open <see cref="Stream"/> of given built-in font.
    /// </summary>
    /// <param name="name">Name of built-in font.</param>
    /// <param name="weight">Weight of font.</param>
    /// <param name="style">Style of font.</param>
    /// <returns><see cref="Stream"/> of given built-in font.</returns>
    public static Stream OpenStream(string name, FontWeight weight = FontWeight.Regular, FontStyle style = FontStyle.Normal)
    {
        var uri = name switch
        {
            nameof(Inter) => Global.Run(() =>
            {
                weight = weight switch
                {
                    FontWeight.Bold => weight,
                    _ => FontWeight.Regular,
                };
                if (weight == FontWeight.Regular)
                    return new Uri(baseCoreInterResourceUri, $"{name}-Regular.ttf");
                return new Uri(baseCoreInterResourceUri, $"{name}-{weight}.ttf");
            }),
            nameof(NotoSans) 
                or nameof(NotoSansMono) 
                or nameof(NotoSansSC) 
                or nameof(NotoSansTC)
                or nameof(NotoSerif) => Global.Run(() =>
            {
                weight = weight switch
                {
                    FontWeight.Bold => weight,
                    _ => FontWeight.Regular,
                };
                if (weight == FontWeight.Regular)
                    return new Uri(baseCoreNotoResourceUri, $"{name}-Regular.ttf");
                return new Uri(baseCoreNotoResourceUri, $"{name}-{weight}.ttf");
            }),
            _ => Global.Run(() =>
            {
                weight = weight switch
                {
                    FontWeight.Bold or FontWeight.Light => weight,
                    _ => FontWeight.Regular,
                };
                style = style switch
                {
                    FontStyle.Italic => style,
                    _ => FontStyle.Normal,
                };
                string postfix;
                if (weight == FontWeight.Regular)
                    postfix = style == FontStyle.Normal ? "Regular" : $"{style}";
                else
                    postfix = style == FontStyle.Normal ? $"{weight}" : $"{weight}{style}";
                return new Uri(baseResourceUri, $"{name}-{postfix}.ttf");
            }),
        };
        return AssetLoader.Open(uri);
    }
    

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