// Test cases are only compiled in Debug configuration and excluded from the released package.
#if DEBUG

using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Threading;
using NUnit.Framework;
using System;
using System.Linq;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Tests of default font setup.
/// </summary>
[TestFixture]
class DefaultFontsTests : ApplicationBasedTests<MockAppSuiteApplication>
{
    // Constants.
    const double FontSize = 14;
    const double LineHeight = FontSize * 1.3;
    const double MetricsTolerance = 0.005;


    /// <summary>
    /// Verify that the character '骨' is rendered by the variant-preferred font of each Chinese variant.
    /// </summary>
    [Test]
    public void ChineseVariantFontSelectionTest() => this.TestOnApplicationThread(() =>
    {
        // check font selected for '骨' of each Chinese variant
        Assert.That(GetFontFamilyNameOfBone(ChineseVariant.Default), Is.EqualTo("Noto Sans SC"));
        Assert.That(GetFontFamilyNameOfBone(ChineseVariant.Taiwan), Is.EqualTo("Noto Sans TC"));
    });


    /// <summary>
    /// Verify that updating 'ContentControlThemeFontFamily' resource dynamically switches the font selected for Chinese text without restarting.
    /// </summary>
    [Test]
    public void DynamicChineseVariantSwitchingTest() => this.TestOnApplicationThread(() =>
    {
        // setup fluent theme and initial resource
        var app = Avalonia.Application.Current.AsNonNull();
        if (!app.Styles.OfType<Avalonia.Themes.Fluent.FluentTheme>().Any())
            app.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        app.Resources["ContentControlThemeFontFamily"] = new FontFamily(AppSuiteApplication.GetDefaultFontFamilyName(ChineseVariant.Default));

        // show window with plain text block
        var textBlock = new Avalonia.Controls.TextBlock { Text = "A骨" };
        var window = new Avalonia.Controls.Window { Content = textBlock };
        window.Show();
        try
        {
            // check font selected with initial variant
            Assert.That(GetFontFamilyNameOfBone(textBlock.TextLayout), Is.EqualTo("Noto Sans SC"));

            // switch variant dynamically then check font selected again
            app.Resources["ContentControlThemeFontFamily"] = new FontFamily(AppSuiteApplication.GetDefaultFontFamilyName(ChineseVariant.Taiwan));
            Dispatcher.UIThread.RunJobs();
            Assert.That(GetFontFamilyNameOfBone(textBlock.TextLayout), Is.EqualTo("Noto Sans TC"));
        }
        finally
        {
            window.Close();
        }
    });


    /// <summary>
    /// Verify that Latin-only and Chinese-only text produce consistent line metrics.
    /// </summary>
    [Test]
    public void ConsistentLineMetricsTest() => this.TestOnApplicationThread(() =>
    {
        // check line metrics of each Chinese variant
        foreach (var chineseVariant in new[] { ChineseVariant.Default, ChineseVariant.Taiwan })
        {
            // create text layouts with explicit line height
            var typeface = new Typeface(new FontFamily(AppSuiteApplication.GetDefaultFontFamilyName(chineseVariant)));
            using var latinLayout = new TextLayout("Hello", typeface, FontSize, Brushes.Black, lineHeight: LineHeight);
            using var chineseLayout = new TextLayout("中文骨", typeface, FontSize, Brushes.Black, lineHeight: LineHeight);

            // check line metrics
            var latinLine = latinLayout.TextLines[0];
            var chineseLine = chineseLayout.TextLines[0];
            Assert.That(chineseLine.Height, Is.EqualTo(latinLine.Height).Within(0.1), $"Line height of Chinese-only text is inconsistent with Latin-only text with variant {chineseVariant}.");
            Assert.That(chineseLine.Baseline, Is.EqualTo(latinLine.Baseline).Within(0.1), $"Baseline of Chinese-only text is inconsistent with Latin-only text with variant {chineseVariant}.");

            // create text layouts without explicit line height
            using var naturalLatinLayout = new TextLayout("Hello", typeface, FontSize, Brushes.Black);
            using var naturalChineseLayout = new TextLayout("中文骨", typeface, FontSize, Brushes.Black);

            // check line metrics
            var naturalLatinLine = naturalLatinLayout.TextLines[0];
            var naturalChineseLine = naturalChineseLayout.TextLines[0];
            Assert.That(naturalChineseLine.Height, Is.EqualTo(naturalLatinLine.Height).Within(0.1), $"Natural line height of Chinese-only text is inconsistent with Latin-only text with variant {chineseVariant}.");
            Assert.That(naturalChineseLine.Baseline, Is.EqualTo(naturalLatinLine.Baseline).Within(0.1), $"Natural baseline of Chinese-only text is inconsistent with Latin-only text with variant {chineseVariant}.");
        }
    });


    // Get family name of font which is selected to render the character '骨'.
    static string GetFontFamilyNameOfBone(ChineseVariant chineseVariant)
    {
        var typeface = new Typeface(new FontFamily(AppSuiteApplication.GetDefaultFontFamilyName(chineseVariant)));
        using var layout = new TextLayout("A骨", typeface, FontSize, Brushes.Black);
        return GetFontFamilyNameOfBone(layout);
    }


    // Get family name of font which is selected to render the character '骨' in given text layout.
    static string GetFontFamilyNameOfBone(TextLayout layout)
    {
        // find the shaped run which renders '骨'
        foreach (var line in layout.TextLines)
        {
            foreach (var run in line.TextRuns)
            {
                if (run is ShapedTextRun shapedRun && shapedRun.Text.Span.Contains('骨'))
                    return shapedRun.GlyphRun.GlyphTypeface.FamilyName;
            }
        }
        throw new AssertionException("No shaped text run for '骨' found.");
    }


    /// <summary>
    /// Verify that vertical metrics of CJK fonts are normalized to be consistent with Inter.
    /// </summary>
    [Test]
    public void NormalizedVerticalMetricsTest() => this.TestOnApplicationThread(() =>
    {
        // get metrics of Inter as reference
        var fontManager = FontManager.Current;
        Assert.That(fontManager.TryGetGlyphTypeface(new Typeface(new FontFamily("fonts:Inter#Inter")), out var interGlyphTypeface), "Unable to get glyph typeface of Inter.");
        var emHeight = (double)interGlyphTypeface!.Metrics.DesignEmHeight;
        var ascentRatio = interGlyphTypeface.Metrics.Ascent / emHeight;
        var descentRatio = interGlyphTypeface.Metrics.Descent / emHeight;
        var lineGapRatio = interGlyphTypeface.Metrics.LineGap / emHeight;

        // check metrics of each normalized CJK font
        foreach (var familyName in new[] { "Noto Sans SC", "Noto Sans TC" })
        {
            foreach (var weight in new[] { FontWeight.Normal, FontWeight.Bold })
            {
                // get glyph typeface
                var typeface = new Typeface(new FontFamily($"fonts:Noto#{familyName}"), weight: weight);
                Assert.That(fontManager.TryGetGlyphTypeface(typeface, out var glyphTypeface), $"Unable to get glyph typeface of {familyName} with weight {weight}.");
                Assert.That(glyphTypeface!.FamilyName, Is.EqualTo(familyName));

                // check metrics
                emHeight = glyphTypeface.Metrics.DesignEmHeight;
                Assert.That(glyphTypeface.Metrics.Ascent / emHeight, Is.EqualTo(ascentRatio).Within(MetricsTolerance), $"Ascent of {familyName} with weight {weight} is not normalized.");
                Assert.That(glyphTypeface.Metrics.Descent / emHeight, Is.EqualTo(descentRatio).Within(MetricsTolerance), $"Descent of {familyName} with weight {weight} is not normalized.");
                Assert.That(glyphTypeface.Metrics.LineGap / emHeight, Is.EqualTo(lineGapRatio).Within(MetricsTolerance), $"Line gap of {familyName} with weight {weight} is not normalized.");
            }
        }
    });


    /// <summary>
    /// Setup embedded font collections.
    /// </summary>
    [OneTimeSetUp]
    public void SetupFontCollections() => this.TestOnApplicationThread(() =>
        AppSuiteApplication.AddEmbeddedFontCollections(FontManager.Current));
}

#endif
