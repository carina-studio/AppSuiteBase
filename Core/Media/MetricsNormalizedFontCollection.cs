using Avalonia.Media.Fonts;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.IO;

namespace CarinaStudio.AppSuite.Media;

/// <summary>
/// Font collection which loads embedded fonts and normalizes vertical metrics of specific font files to make their line metrics consistent with the default Latin font.
/// </summary>
internal class MetricsNormalizedFontCollection : FontCollectionBase
{
    /// <summary>
    /// Initialize new <see cref="MetricsNormalizedFontCollection"/> instance.
    /// </summary>
    /// <param name="key">Key of the font collection.</param>
    /// <param name="source">Base URI of embedded font assets.</param>
    /// <param name="normalizedFontFileNames">Name of font files to normalize their vertical metrics.</param>
    public MetricsNormalizedFontCollection(Uri key, Uri source, IEnumerable<string> normalizedFontFileNames)
    {
        // setup key
        this.Key = key;

        // load embedded font assets and register glyph typefaces
        var normalizedFontFileNameSet = new HashSet<string>(normalizedFontFileNames, StringComparer.OrdinalIgnoreCase);
        foreach (var fontAsset in AssetLoader.GetAssets(source, null))
        {
            // skip non-font assets
            var fontFileName = Path.GetFileName(fontAsset.AbsolutePath);
            if (!fontFileName.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) && !fontFileName.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
                continue;

            // open asset and normalize vertical metrics if needed
            using var assetStream = AssetLoader.Open(fontAsset);
            var fontStream = assetStream;
            if (normalizedFontFileNameSet.Contains(fontFileName))
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
                this.TryAddGlyphTypeface(fontStream, out _);
            }
            finally
            {
                if (!ReferenceEquals(fontStream, assetStream))
                    fontStream.Dispose();
            }
        }
    }


    /// <inheritdoc/>
    public override Uri Key { get; }
}
