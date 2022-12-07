using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.Styling;
using CarinaStudio.Threading;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Service of syntax highlighting.
/// </summary>
public static class SyntaxHighlighting
{
    // Fields.
    static IAppSuiteApplication? app;
    static ThemeMode resourcesThemeMode;
    static IDisposable? resourcesToken;
    static CultureInfo? stringsCulture;
    static IDisposable? stringsToken;


    /// <summary>
    /// Initialize asynchronously.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <returns>Task of initialization.</returns>
    public static Task InitializeAsync(IAppSuiteApplication app)
    {
        // check state
        app.VerifyAccess();
        if (SyntaxHighlighting.app != null)
        {
            if (SyntaxHighlighting.app != app)
                throw new InvalidOperationException();
            return Task.CompletedTask;
        }
        SyntaxHighlighting.app = app;

        // load base strings
        var baseUri = new Uri($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}");
        app.AddCustomResource(new ResourceInclude()
        {
            Source = new(baseUri, "/Strings/Default.axaml")
        });

        // load base styles
        app.AddCustomStyle(new StyleInclude(baseUri)
        {
            Source = new(baseUri, "/Themes/Base.axaml")
        });

        // load strings
        app.LoadingStrings += (IAppSuiteApplication? app, CultureInfo cultureInfo) => UpdateStrings();
        UpdateStrings();

        // load theme resources
        app.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(app.EffectiveThemeMode))
                UpdateThemeResources();
        };
        UpdateThemeResources();

        // complete
        return Task.CompletedTask;
    }


    // Update strings for current culture.
    static void UpdateStrings()
    {
        var cultureInfo = app!.CultureInfo;
        if (stringsCulture?.Name == cultureInfo.Name)
            return;
        stringsToken = stringsToken.DisposeAndReturnNull();
        var stringResources = Global.Run(() =>
        {
            var name = cultureInfo.Name;
            var baseUri = new Uri($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}");
            if (name.StartsWith("zh-"))
            {
                if (name.EndsWith("TW"))
                    return new ResourceInclude() { Source = new(baseUri, "/Strings/zh-TW.axaml") };
                return new ResourceInclude() { Source = new(baseUri, "/Strings/zh-CN.axaml") };
            }
            return null;
        });
        if (stringResources != null)
            stringsToken = app.AddCustomResource(stringResources);
        stringsCulture = cultureInfo;
    }


    // Update resources for current theme mode.
    static void UpdateThemeResources()
    {
        var themeMode = app!.EffectiveThemeMode;
        if (resourcesToken != null && resourcesThemeMode == themeMode)
            return;
        var baseUri = new Uri($"avares://{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}");
        resourcesToken = resourcesToken.DisposeAndReturnNull();
        resourcesToken = app.AddCustomResource(themeMode switch
        {
            ThemeMode.Light => new ResourceInclude() { Source = new(baseUri, "/Themes/Light.axaml") },
            _ => new ResourceInclude() { Source = new(baseUri, "/Themes/Dark.axaml") },
        });
        resourcesThemeMode = themeMode;
    }


    /// <summary>
    /// Throw <see cref="InvalidOperationException"/> if syntax highlighting service is not initialized yet.
    /// </summary>
    public static void VerifyInitialization()
    {
        if (app == null)
            throw new InvalidOperationException("Syntax highlighting service is not initialized yet.");
    }
}