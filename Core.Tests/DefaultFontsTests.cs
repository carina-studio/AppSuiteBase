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
    const char CjkFallbackChar = '业'; // covered by Noto Sans SC and TC but not by Noto Sans JP
    const char CjkVariantChar = '骨'; // covered by all CJK fonts but its glyph differs between them
    const double FontSize = 14;
    const double LineHeight = FontSize * 1.3;
    const double MetricsTolerance = 0.005;


    /// <summary>
    /// Verify that Latin-only and CJK-only text produce consistent line metrics.
    /// </summary>
    [Test]
    public void ConsistentLineMetricsTest() => this.TestOnApplicationThread(() =>
    {
        // check line metrics of each culture
        foreach (var (culture, cjkText) in new[] { (ApplicationCulture.ZH_CN, "中文骨"), (ApplicationCulture.ZH_TW, "中文骨"), (ApplicationCulture.JA_JP, "日本語骨") })
        {
            // create text layouts with explicit line height
            var typeface = new Typeface(new FontFamily(GetDefaultFontFamilyName(culture)));
            using var latinLayout = new TextLayout("Hello", typeface, FontSize, Brushes.Black, lineHeight: LineHeight);
            using var cjkLayout = new TextLayout(cjkText, typeface, FontSize, Brushes.Black, lineHeight: LineHeight);

            // check line metrics
            var latinLine = latinLayout.TextLines[0];
            var cjkLine = cjkLayout.TextLines[0];
            Assert.That(cjkLine.Height, Is.EqualTo(latinLine.Height).Within(0.1), $"Line height of CJK-only text is inconsistent with Latin-only text with culture {culture}.");
            Assert.That(cjkLine.Baseline, Is.EqualTo(latinLine.Baseline).Within(0.1), $"Baseline of CJK-only text is inconsistent with Latin-only text with culture {culture}.");

            // create text layouts without explicit line height
            using var naturalLatinLayout = new TextLayout("Hello", typeface, FontSize, Brushes.Black);
            using var naturalCjkLayout = new TextLayout(cjkText, typeface, FontSize, Brushes.Black);

            // check line metrics
            var naturalLatinLine = naturalLatinLayout.TextLines[0];
            var naturalCjkLine = naturalCjkLayout.TextLines[0];
            Assert.That(naturalCjkLine.Height, Is.EqualTo(naturalLatinLine.Height).Within(0.1), $"Natural line height of CJK-only text is inconsistent with Latin-only text with culture {culture}.");
            Assert.That(naturalCjkLine.Baseline, Is.EqualTo(naturalLatinLine.Baseline).Within(0.1), $"Natural baseline of CJK-only text is inconsistent with Latin-only text with culture {culture}.");
        }
    });


    /// <summary>
    /// Verify that the character '骨' is rendered by the font preferred by each culture.
    /// </summary>
    [Test]
    public void CultureFontSelectionTest() => this.TestOnApplicationThread(() =>
    {
        // check font selected for '骨' of each culture
        Assert.That(GetFontFamilyNameOf(CjkVariantChar, GetDefaultFontFamilyName(ApplicationCulture.ZH_CN)), Is.EqualTo("Noto Sans SC"));
        Assert.That(GetFontFamilyNameOf(CjkVariantChar, GetDefaultFontFamilyName(ApplicationCulture.ZH_TW)), Is.EqualTo("Noto Sans TC"));
        Assert.That(GetFontFamilyNameOf(CjkVariantChar, GetDefaultFontFamilyName(ApplicationCulture.JA_JP)), Is.EqualTo("Noto Sans JP"));
    });


    /// <summary>
    /// Verify that updating 'ContentControlThemeFontFamily' resource dynamically switches the font selected for CJK text without restarting.
    /// </summary>
    [Test]
    public void DynamicCultureFontSwitchingTest() => this.TestOnApplicationThread(() =>
    {
        // setup fluent theme and initial resource
        var app = Avalonia.Application.Current.AsNonNull();
        if (!app.Styles.OfType<Avalonia.Themes.Fluent.FluentTheme>().Any())
            app.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        app.Resources["ContentControlThemeFontFamily"] = new FontFamily(GetDefaultFontFamilyName(ApplicationCulture.ZH_CN));

        // show window with plain text block
        var textBlock = new Avalonia.Controls.TextBlock { Text = "A骨" };
        var window = new Avalonia.Controls.Window { Content = textBlock };
        window.Show();
        try
        {
            // check font selected with initial culture
            Assert.That(GetFontFamilyNameOf(CjkVariantChar, textBlock.TextLayout), Is.EqualTo("Noto Sans SC"));

            // switch culture dynamically then check font selected again
            app.Resources["ContentControlThemeFontFamily"] = new FontFamily(GetDefaultFontFamilyName(ApplicationCulture.ZH_TW));
            Dispatcher.UIThread.RunJobs();
            Assert.That(GetFontFamilyNameOf(CjkVariantChar, textBlock.TextLayout), Is.EqualTo("Noto Sans TC"));

            // switch to Japanese then check font selected again
            app.Resources["ContentControlThemeFontFamily"] = new FontFamily(GetDefaultFontFamilyName(ApplicationCulture.JA_JP));
            Dispatcher.UIThread.RunJobs();
            Assert.That(GetFontFamilyNameOf(CjkVariantChar, textBlock.TextLayout), Is.EqualTo("Noto Sans JP"));
        }
        finally
        {
            window.Close();
        }
    });


    /// <summary>
    /// Verify that only the first CJK font of the composite font family affects font selection.
    /// </summary>
    /// <remarks>
    /// When the first CJK font doesn't cover the character, matching stops following the composite and resolves the
    /// character by an unordered scan of the whole font collection, which always lands on 'Noto Sans SC' here. The ordering
    /// of the remaining CJK fonts therefore has no effect, and neither does their presence in the composite at all. This
    /// still holds on Avalonia 12.0.4: PR #21435 fixed first-position selection but did not make the remaining positions
    /// ordered. The test is a tripwire — it is expected to start failing once matching does become ordered, at which point
    /// the assertions should be re-pointed at the ordered behavior. See the ordering caveat in Core/AGENTS.md.
    /// </remarks>
    [Test]
    public void FallbackFontSelectionTest() => this.TestOnApplicationThread(() =>
    {
        // check that the first CJK font is used when it covers the character
        Assert.That(GetFontFamilyNameOf(CjkFallbackChar, "fonts:Inter#Inter, fonts:Noto#Noto Sans TC, fonts:Noto#Noto Sans SC"), Is.EqualTo("Noto Sans TC"));

        // check that the remaining CJK fonts are ignored when the first one doesn't cover the character
        Assert.That(GetFontFamilyNameOf(CjkFallbackChar, "fonts:Inter#Inter, fonts:Noto#Noto Sans JP, fonts:Noto#Noto Sans SC, fonts:Noto#Noto Sans TC"), Is.EqualTo("Noto Sans SC"));
        Assert.That(GetFontFamilyNameOf(CjkFallbackChar, "fonts:Inter#Inter, fonts:Noto#Noto Sans JP, fonts:Noto#Noto Sans TC, fonts:Noto#Noto Sans SC"), Is.EqualTo("Noto Sans SC"));
        Assert.That(GetFontFamilyNameOf(CjkFallbackChar, "fonts:Inter#Inter, fonts:Noto#Noto Sans JP"), Is.EqualTo("Noto Sans SC"));
    });


    // Get name of default font family for given application culture.
    static string GetDefaultFontFamilyName(ApplicationCulture culture) =>
        AppSuiteApplication.GetDefaultFontFamilyName(culture.GetCultureInfo());


    // Get family name of font which is selected to render given character with given font family.
    static string GetFontFamilyNameOf(char c, string fontFamilyName)
    {
        var typeface = new Typeface(new FontFamily(fontFamilyName));
        using var layout = new TextLayout($"A{c}", typeface, FontSize, Brushes.Black);
        return GetFontFamilyNameOf(c, layout);
    }


    // Get family name of font which is selected to render given character in given text layout.
    static string GetFontFamilyNameOf(char c, TextLayout layout)
    {
        // find the shaped run which renders the character
        foreach (var line in layout.TextLines)
        {
            foreach (var run in line.TextRuns)
            {
                if (run is ShapedTextRun shapedRun && shapedRun.Text.Span.Contains(c))
                    return shapedRun.GlyphRun.GlyphTypeface.FamilyName;
            }
        }
        throw new AssertionException($"No shaped text run for '{c}' found.");
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
        foreach (var familyName in new[] { "Noto Sans JP", "Noto Sans SC", "Noto Sans TC" })
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
