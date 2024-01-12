using Avalonia.Markup.Xaml.Styling;
using CarinaStudio.Threading;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
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
        
        // prevent trimming compiled Avalonia XAML
        KeepCompiledAvaloniaXaml();

        // load base strings
        var baseUri = new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}");
#pragma warning disable IL2026
        app.AddCustomResource(new ResourceInclude(baseUri)
        {
            Source = new("Strings/Default.axaml", UriKind.Relative)
        });
#pragma warning restore IL2026

        // load base styles
#pragma warning disable IL2026
        app.AddCustomStyle(new StyleInclude(baseUri)
        {
            Source = new("Themes/Base.axaml", UriKind.Relative)
        });
#pragma warning restore IL2026

        // load strings
#pragma warning disable CS8622
        app.LoadingStrings += (_, _) => UpdateStrings();
#pragma warning restore CS8622
        UpdateStrings();

        // load theme resources
#pragma warning disable CS8622
        app.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(app.EffectiveThemeMode))
                UpdateThemeResources();
        };
#pragma warning restore CS8622
        UpdateThemeResources();

        // complete
        return Task.CompletedTask;
    }
    
    
    // Touch compiled Avalonia XAML to prevent being trimmed.
    static void KeepCompiledAvaloniaXaml()
    {
        static void KeepTypeFromTrimming(Assembly assembly, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string typeName)
        {
#pragma warning disable IL2026
            var type = assembly.GetType(typeName);
#pragma warning restore IL2026
            if (type is null)
                throw new InternalStateCorruptedException("Compiled Avalonia XAML not found.");
        }
        var assembly = Assembly.GetCallingAssembly();
        KeepTypeFromTrimming(assembly, "CompiledAvaloniaXaml.!AvaloniaResources");
        KeepTypeFromTrimming(assembly, "CompiledAvaloniaXaml.!XamlLoader");
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
            var baseUri = new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}");
            if (name.StartsWith("zh-"))
            {
#pragma warning disable IL2026
                if (name.EndsWith("TW"))
                    return new ResourceInclude(baseUri) { Source = new("Strings/zh-TW.axaml", UriKind.Relative) };
                return new ResourceInclude(baseUri) { Source = new("Strings/zh-CN.axaml", UriKind.Relative) };
#pragma warning restore IL2026
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
        var baseUri = new Uri($"avares://{Assembly.GetExecutingAssembly().GetName().Name}");
        resourcesToken = resourcesToken.DisposeAndReturnNull();
#pragma warning disable IL2026
        resourcesToken = app.AddCustomResource(themeMode switch
        {
            ThemeMode.Light => new ResourceInclude(baseUri) { Source = new("Themes/Light.axaml", UriKind.Relative) },
            _ => new ResourceInclude(baseUri) { Source = new("Themes/Dark.axaml", UriKind.Relative) },
        });
#pragma warning restore IL2026
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