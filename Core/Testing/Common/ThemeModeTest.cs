using Avalonia.Media;
using CarinaStudio.Configuration;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.Common;

class ThemeModeTest : TestCase
{
    // Static fields.
    static readonly Color TestColorDark = new(0x7f, 0xaa, 0xbb, 0xcc);
    static readonly Color TestColorLight = new(0x70, 0x33, 0x22, 0x11);


    // Fields.
    ThemeMode initThemeMode;


    // Constructor.
    public ThemeModeTest(IAppSuiteApplication app) : base(app, TestCaseCategoryNames.Common, "Theme Mode")
    { }


    /// <inheritdoc/>
    protected override async Task OnRunAsync(CancellationToken cancellationToken)
    {
        // check system theme mode
        bool shouldSystemThemeModeSupported;
        if (Platform.IsNotLinux)
            shouldSystemThemeModeSupported = true;
        else if (this.Application is AppSuiteApplication asApp && asApp.IsSystemThemeModeSupportedOnLinux)
            shouldSystemThemeModeSupported = true;
        else
            shouldSystemThemeModeSupported = false;
        if (shouldSystemThemeModeSupported)
        {
            Assert.That(this.Application.IsSystemThemeModeSupported, "System theme mode should be supported.");
            if (this.Application is AppSuiteApplication appSuiteApp)
            {
                var sysThemeMode = await appSuiteApp.GetSystemThemeModeAsync();
                if (this.Application.Settings.GetValueOrDefault(SettingKeys.ThemeMode) != ThemeMode.System)
                {
                    this.Application.Settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, ThemeMode.System);
                    if (this.Application.IsRestartingRootWindowsNeeded)
                    {
                        var mainWindowCount = this.Application.MainWindows.Count;
                        await this.Application.RestartRootWindowsAsync();
                        await WaitForConditionAsync(() => this.Application.MainWindows.Count == mainWindowCount,
                            "Unable to restart main windows after changing to system theme mode.", 
                            cancellationToken);
                    }
                }
                Assert.That(sysThemeMode == this.Application.EffectiveThemeMode, "System theme mode is not an expected value.");
            }
        }
        else
            Assert.That(!this.Application.IsSystemThemeModeSupported, "System theme mode should not be supported.");
        
        // check dark mode
        if (this.Application.Settings.GetValueOrDefault(SettingKeys.ThemeMode) != ThemeMode.Dark)
        {
            this.Application.Settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, ThemeMode.Dark);
            if (this.Application.IsRestartingRootWindowsNeeded)
            {
                var mainWindowCount = this.Application.MainWindows.Count;
                await this.Application.RestartRootWindowsAsync();
                await WaitForConditionAsync(() => this.Application.MainWindows.Count == mainWindowCount,
                    "Unable to restart main windows after changing to dark theme mode.", 
                    cancellationToken);
            }
        }
        var color = this.Application.FindResourceOrDefault<Color>("Color/ThemeModeTest.Color");
        Assert.That(TestColorDark == color, "Content of dark theme is incorrect.");

        // check light mode
        this.Application.Settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, ThemeMode.Light);
        if (this.Application.IsRestartingRootWindowsNeeded)
        {
            var mainWindowCount = this.Application.MainWindows.Count;
            await this.Application.RestartRootWindowsAsync();
            await WaitForConditionAsync(() => this.Application.MainWindows.Count == mainWindowCount,
                "Unable to restart main windows after changing to light theme mode.", 
                cancellationToken);
        }
        color = this.Application.FindResourceOrDefault<Color>("Color/ThemeModeTest.Color");
        Assert.That(TestColorLight == color, "Content of light theme is incorrect.");
    }


    /// <inheritdoc/>
    protected override Task OnSetupAsync()
    {
        this.initThemeMode = this.Application.Settings.GetValueOrDefault(SettingKeys.ThemeMode);
        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    protected override async Task OnTearDownAsync()
    {
        this.Application.Settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, this.initThemeMode);
        if (this.Application.IsRestartingRootWindowsNeeded)
        {
            var mainWindowCount = this.Application.MainWindows.Count;
            await this.Application.RestartRootWindowsAsync();
            await WaitForConditionAsync(() => this.Application.MainWindows.Count == mainWindowCount,
                "Unable to restart main windows after restoring theme mode.", 
                CancellationToken.None);
        }
    }
}