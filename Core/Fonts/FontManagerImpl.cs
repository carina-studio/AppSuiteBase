using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Skia;
using CarinaStudio.Collections;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Reflection;

namespace CarinaStudio.AppSuite.Fonts;

class FontManagerImpl : IFontManagerImpl
{
    // Constants.
    const string DefaultFontFamilyName = "Inter";


    // Fields.
    readonly IFontManagerImpl defaultFontManager;
    GlyphTypefaceImpl? defaultGlyphTypeface;
    GlyphTypefaceImpl? defaultGlyphTypefaceBold;
    readonly HashSet<string> installedFontFamilyNames = new();


    // Constructor.
    public FontManagerImpl()
    {
        var assembly = Assembly.Load("Avalonia.Skia");
        var defaultFontManagerType = assembly.GetType("Avalonia.Skia.FontManagerImpl").AsNonNull();
        this.defaultFontManager = (IFontManagerImpl)Activator.CreateInstance(defaultFontManagerType).AsNonNull();
    }


    /// <inheritdoc/>
    public IGlyphTypeface CreateGlyphTypeface(Typeface typeface)
    {
        // create typeface for given font family
        var fontFamilyName = typeface.FontFamily.FamilyNames[0];
        switch (fontFamilyName)
        {
            case "$Default":
            case DefaultFontFamilyName:
                break;
            default:
                if (this.installedFontFamilyNames.IsEmpty())
                    this.GetInstalledFontFamilyNames(true);
                if (this.installedFontFamilyNames.Contains(fontFamilyName))
                    return this.defaultFontManager.CreateGlyphTypeface(typeface);
                break;
        }

        // create default type face
        var isBold = (typeface.Weight == FontWeight.Bold);
        string fontFileName;
        if (isBold)
        {
            if (this.defaultGlyphTypefaceBold is not null)
                return this.defaultGlyphTypefaceBold;
            fontFileName = "Inter-Bold.ttf";
        }
        else
        {
            if (this.defaultGlyphTypeface is not null)
                return this.defaultGlyphTypeface;
            fontFileName = "Inter-Regular.ttf";
        }
        var assetLoader = AvaloniaLocator.Current.GetService(typeof(IAssetLoader)) as IAssetLoader;
        using var stream = assetLoader?.Open(new($"avares://CarinaStudio.AppSuite.Core/Fonts/{fontFileName}")).AsNonNull();
        var skiaTypeface = SKTypeface.FromStream(stream);
        if (isBold)
        {
            this.defaultGlyphTypefaceBold = new GlyphTypefaceImpl(skiaTypeface, FontSimulations.None);
            return this.defaultGlyphTypefaceBold;
        }
        else
        {
            this.defaultGlyphTypeface = new GlyphTypefaceImpl(skiaTypeface, FontSimulations.None);
            return this.defaultGlyphTypeface;
        }
    }
        

    /// <inheritdoc/>
    public string GetDefaultFontFamilyName() => DefaultFontFamilyName;


    /// <inheritdoc/>
    public IEnumerable<string> GetInstalledFontFamilyNames(bool checkForUpdates = false)
    {
        if (this.installedFontFamilyNames.IsEmpty() || checkForUpdates)
        {
            this.installedFontFamilyNames.Clear();
            this.installedFontFamilyNames.AddAll(this.defaultFontManager.GetInstalledFontFamilyNames(true));
        }
        return this.installedFontFamilyNames.ToArray();
    }


    /// <inheritdoc/>
    public bool TryMatchCharacter(int codepoint, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, FontFamily? fontFamily, CultureInfo? culture, out Typeface typeface) =>
        this.defaultFontManager.TryMatchCharacter(codepoint, fontStyle, fontWeight, fontStretch, fontFamily, culture, out typeface);
}