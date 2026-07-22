using Avalonia.Media;
using Avalonia.Media.Fonts;
using Avalonia.Platform;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Font collection which loads embedded fonts and normalizes vertical metrics of specific font files to make their line metrics consistent with the default Latin font. The implementation is ported from <see cref="EmbeddedFontCollection"/> of Avalonia 11.3.
/// </summary>
internal class MetricsNormalizedFontCollection(Uri key, Uri source, IEnumerable<string> normalizedFontFileNames) : FontCollectionBase
{
    // Static fields.
    static readonly PropertyInfo GlyphTypeface2FamilyNamesProperty;
    static readonly Type GlyphTypeface2Type;
    static readonly PropertyInfo GlyphTypeface2TypographicFamilyNameProperty;
    static readonly MethodInfo TryCreateGlyphTypefaceFromStreamMethod;


    // Fields.
    readonly List<FontFamily> fontFamilies = new(1);
    readonly HashSet<string> normalizedFontFileNames = new(normalizedFontFileNames, StringComparer.OrdinalIgnoreCase);


    // Static initializer.
    // AsNonNull() is intentional: these members are hidden in reference assemblies but available at runtime. A crash here on Avalonia upgrade is the signal that they moved and this class needs revisiting.
    static MetricsNormalizedFontCollection()
    {
        GlyphTypeface2Type = Type.GetType("Avalonia.Media.IGlyphTypeface2, Avalonia.Base").AsNonNull();
        GlyphTypeface2FamilyNamesProperty = GlyphTypeface2Type.GetProperty("FamilyNames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).AsNonNull();
        GlyphTypeface2TypographicFamilyNameProperty = GlyphTypeface2Type.GetProperty("TypographicFamilyName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).AsNonNull();
        TryCreateGlyphTypefaceFromStreamMethod = typeof(IFontManagerImpl).GetMethod("TryCreateGlyphTypeface", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, [typeof(Stream), typeof(FontSimulations), typeof(IGlyphTypeface).MakeByRefType()]).AsNonNull();
    }


    // Register given glyph typeface with all of its family names.
    void AddGlyphTypeface(IGlyphTypeface glyphTypeface)
    {
        // register with typographic family name and all localized family names
        if (GlyphTypeface2Type.IsInstanceOfType(glyphTypeface))
        {
            var typographicFamilyName = (string?)GlyphTypeface2TypographicFamilyNameProperty.GetValue(glyphTypeface);
            if (!string.IsNullOrEmpty(typographicFamilyName))
                this.AddGlyphTypefaceByFamilyName(typographicFamilyName, glyphTypeface);
            foreach (var (_, familyName) in (IEnumerable<KeyValuePair<ushort, string>>)GlyphTypeface2FamilyNamesProperty.GetValue(glyphTypeface).AsNonNull())
                this.AddGlyphTypefaceByFamilyName(familyName, glyphTypeface);
        }
        else
#pragma warning disable CS0618 // Type or member is obsolete
            this.AddGlyphTypefaceByFamilyName(glyphTypeface.FamilyName, glyphTypeface);
#pragma warning restore CS0618 // Type or member is obsolete
    }


    // Register given glyph typeface with given family name.
    void AddGlyphTypefaceByFamilyName(string familyName, IGlyphTypeface glyphTypeface)
    {
        var typefaces = this._glyphTypefaceCache.GetOrAdd(familyName, _ =>
        {
            this.fontFamilies.Add(new FontFamily(key, familyName));
            return new ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?>();
        });
#pragma warning disable CS0618 // Type or member is obsolete
        typefaces.TryAdd(new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch), glyphTypeface);
#pragma warning restore CS0618 // Type or member is obsolete
    }


    /// <inheritdoc/>
    public override int Count => this.fontFamilies.Count;


    /// <inheritdoc/>
    public override IEnumerator<FontFamily> GetEnumerator() => this.fontFamilies.GetEnumerator();


    // Extract implicit style/weight/stretch terms from family name of given typeface.
    static Typeface GetImplicitTypeface(Typeface typeface, out string normalizedFamilyName)
    {
        // return directly if there is no extra term in family name
        var familyName = typeface.FontFamily.FamilyNames.PrimaryFamilyName;
        normalizedFamilyName = familyName;
        if (!familyName.Contains(' '))
            return typeface;

        // extract style/weight/stretch terms from family name. The leading token is always treated as part of the family name.
        var tokens = familyName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var style = typeface.Style;
        var weight = typeface.Weight;
        var stretch = typeface.Stretch;
        var unmatchedTokenCount = 1;
        for (var i = 1; i < tokens.Length; ++i)
        {
            // keep numeric token as part of the family name
            var token = tokens[i];
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
            {
                tokens[unmatchedTokenCount++] = token;
                continue;
            }

            // match token with style, weight or stretch
            if (Enum.TryParse<FontStyle>(token, true, out var parsedStyle) && Enum.IsDefined(parsedStyle))
                style = parsedStyle;
            else if (Enum.TryParse<FontWeight>(token, true, out var parsedWeight) && Enum.IsDefined(parsedWeight))
                weight = parsedWeight;
            else if (Enum.TryParse<FontStretch>(token, true, out var parsedStretch) && Enum.IsDefined(parsedStretch))
                stretch = parsedStretch;
            else
                tokens[unmatchedTokenCount++] = token;
        }

        // build normalized family name and typeface
        if (unmatchedTokenCount < tokens.Length)
            normalizedFamilyName = string.Join(' ', tokens.Take(unmatchedTokenCount));
        return new Typeface(typeface.FontFamily, style, weight, stretch);
    }


    /// <inheritdoc/>
    public override void Initialize(IFontManagerImpl fontManager)
    {
        // load embedded font assets and register glyph typefaces
        foreach (var fontAsset in FontFamilyLoader.LoadFontAssets(source))
        {
            // open asset and normalize vertical metrics if needed
            using var assetStream = AssetLoader.Open(fontAsset);
            var fontStream = assetStream;
            if (this.normalizedFontFileNames.Contains(Path.GetFileName(fontAsset.AbsolutePath)))
            {
                var fontData = new MemoryStream().Use(memoryStream =>
                {
                    assetStream.CopyTo(memoryStream);
                    return memoryStream.ToArray();
                });
                FontVerticalMetricsNormalizer.Normalize(fontData);
                fontStream = new MemoryStream(fontData);
            }

            // create and register glyph typeface
            try
            {
                var invocationArgs = new object?[] { fontStream, FontSimulations.None, null };
                if ((bool)TryCreateGlyphTypefaceFromStreamMethod.Invoke(fontManager, invocationArgs).AsNonNull() && invocationArgs[2] is IGlyphTypeface glyphTypeface)
                    this.AddGlyphTypeface(glyphTypeface);
            }
            finally
            {
                if (!ReferenceEquals(fontStream, assetStream))
                    fontStream.Dispose();
            }
        }
    }


    /// <inheritdoc/>
    public override FontFamily this[int index] => this.fontFamilies[index];


    /// <inheritdoc/>
    public override Uri Key => key;


    // Find fallback glyph typeface with nearest stretch.
    static bool TryFindStretchFallback(ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces, FontCollectionKey key, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
    {
        // search from nearest stretch toward the opposite end
        glyphTypeface = null;
        var stretch = (int)key.Stretch;
        if (stretch < 5)
        {
            for (var i = 0; stretch + i < 9; ++i)
            {
                if (glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)(stretch + i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
        }
        else
        {
            for (var i = 0; stretch - i > 1; ++i)
            {
                if (glyphTypefaces.TryGetValue(key with { Stretch = (FontStretch)(stretch - i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
        }
        return false;
    }


    // Find fallback glyph typeface with nearest weight.
    static bool TryFindWeightFallback(ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces, FontCollectionKey key, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
    {
        // if the target weight is between 400 and 500 inclusive, look for available weights between the target and 500 first, then less than the target, then greater than 500
        glyphTypeface = null;
        var weight = (int)key.Weight;
        if (weight >= 400 && weight <= 500)
        {
            for (var i = 0; weight + i <= 500; i += 50)
            {
                if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
            for (var i = 0; weight - i >= 100; i += 50)
            {
                if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
            for (var i = 0; weight + i <= 900; i += 50)
            {
                if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
        }

        // if the target weight is less than 400, look for available weights less than the target first, then greater than the target
        if (weight < 400)
        {
            for (var i = 0; weight - i >= 100; i += 50)
            {
                if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
            for (var i = 0; weight + i <= 900; i += 50)
            {
                if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
        }

        // if the target weight is greater than 500, look for available weights greater than the target first, then less than the target
        if (weight > 500)
        {
            for (var i = 0; weight + i <= 900; i += 50)
            {
                if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight + i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
            for (var i = 0; weight - i >= 100; i += 50)
            {
                if (glyphTypefaces.TryGetValue(key with { Weight = (FontWeight)(weight - i) }, out glyphTypeface) && glyphTypeface is not null)
                    return true;
            }
        }
        return false;
    }


    /// <inheritdoc/>
    public override bool TryGetGlyphTypeface(string familyName, FontStyle style, FontWeight weight, FontStretch stretch, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
    {
        // extract implicit style/weight/stretch terms from family name
        var typeface = GetImplicitTypeface(new Typeface(familyName, style, weight, stretch), out familyName);
        style = typeface.Style;
        weight = typeface.Weight;
        stretch = typeface.Stretch;

        // find registered or nearest glyph typeface by family name
        var typefaceKey = new FontCollectionKey(style, weight, stretch);
        if (this._glyphTypefaceCache.TryGetValue(familyName, out var glyphTypefaces))
        {
            // use exactly matched glyph typeface
            if (glyphTypefaces.TryGetValue(typefaceKey, out glyphTypeface) && glyphTypeface is not null)
                return true;

            // use nearest glyph typeface and create synthetic one if possible
            if (TryGetNearestMatch(glyphTypefaces, typefaceKey, out glyphTypeface))
            {
#pragma warning disable CS0618 // Type or member is obsolete
                var matchedKey = new FontCollectionKey(glyphTypeface.Style, glyphTypeface.Weight, glyphTypeface.Stretch);
#pragma warning restore CS0618 // Type or member is obsolete
                if (matchedKey != typefaceKey)
                {
                    if (this.TryCreateSyntheticGlyphTypeface(glyphTypeface, style, weight, stretch, out var syntheticGlyphTypeface))
                        glyphTypeface = syntheticGlyphTypeface;
                    else
                        glyphTypefaces.TryAdd(typefaceKey, glyphTypeface);
                }
                return true;
            }
        }

        // find partially matched font family
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < this.fontFamilies.Count; ++i)
        {
            var fontFamily = this.fontFamilies[i];
            if (fontFamily.Name.StartsWith(familyName, StringComparison.OrdinalIgnoreCase)
                && this._glyphTypefaceCache.TryGetValue(fontFamily.Name, out glyphTypefaces)
                && TryGetNearestMatch(glyphTypefaces, typefaceKey, out glyphTypeface))
            {
                return true;
            }
        }
        glyphTypeface = null;
        return false;
    }


    // Find registered glyph typeface with nearest style/weight/stretch.
    static bool TryGetNearestMatch(ConcurrentDictionary<FontCollectionKey, IGlyphTypeface?> glyphTypefaces, FontCollectionKey key, [NotNullWhen(true)] out IGlyphTypeface? glyphTypeface)
    {
        // use exactly matched glyph typeface
        if (glyphTypefaces.TryGetValue(key, out glyphTypeface) && glyphTypeface is not null)
            return true;

        // fall back to normal style then find by nearest stretch and weight
        if (key.Style != FontStyle.Normal)
            key = key with { Style = FontStyle.Normal };
        if (key.Stretch != FontStretch.Normal)
        {
            if (TryFindStretchFallback(glyphTypefaces, key, out glyphTypeface))
                return true;
            if (key.Weight != FontWeight.Normal && TryFindStretchFallback(glyphTypefaces, key with { Weight = FontWeight.Normal }, out glyphTypeface))
                return true;
            key = key with { Stretch = FontStretch.Normal };
        }
        if (TryFindWeightFallback(glyphTypefaces, key, out glyphTypeface))
            return true;
        if (TryFindStretchFallback(glyphTypefaces, key, out glyphTypeface))
            return true;

        // use any registered glyph typeface
        foreach (var registeredGlyphTypeface in glyphTypefaces.Values)
        {
            if (registeredGlyphTypeface is not null)
            {
                glyphTypeface = registeredGlyphTypeface;
                return true;
            }
        }
        return false;
    }
}
