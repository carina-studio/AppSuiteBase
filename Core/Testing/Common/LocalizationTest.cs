using Avalonia.Threading;
using CarinaStudio.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.Common;

class LocalizationTest(IAppSuiteApplication app) : TestCase(app, TestCaseCategoryNames.Common, "Localization")
{
    // Static fields.
    static readonly Dictionary<ApplicationCulture, string> TestStrings = new()
    {
        { ApplicationCulture.EN_US, "English" },
        { ApplicationCulture.ZH_CN, "简体中文" },
        { ApplicationCulture.ZH_TW, "正體中文" },
    };


    // Fields.
    ApplicationCulture initAppCulture;
    

    /// <inheritdoc/>
    protected override async Task OnRunAsync(CancellationToken cancellationToken)
    {
        var isTestStringChanged = false;
        var latestNotifiedTestString = default(string);
        var observableTestString = this.Application.GetObservableString("LocalizationTest.String");
        using var observerToken = observableTestString.Subscribe(s =>
        {
            isTestStringChanged = true;
            latestNotifiedTestString = s;
        });
        var appCultures = new List<ApplicationCulture>(Enum.GetValues<ApplicationCulture>());
        appCultures.Remove(ApplicationCulture.System);
        appCultures[0].Let(it =>
        {
            var sysCultureName = CultureInfo.InstalledUICulture.Name;
            switch (it)
            {
                case ApplicationCulture.EN_US:
                    if (sysCultureName.StartsWith("en-"))
                        appCultures.Reverse();
                    break;
                case ApplicationCulture.ZH_CN:
                    if (sysCultureName.StartsWith("zh-") && sysCultureName.EndsWith("CN"))
                        appCultures.Reverse();
                    break;
                case ApplicationCulture.ZH_TW:
                    if (sysCultureName.StartsWith("zh-") && sysCultureName.EndsWith("TW"))
                        appCultures.Reverse();
                    break;
            }
        });
        foreach (var appCulture in appCultures)
        {
            if (!TestStrings.TryGetValue(appCulture, out var expectedString))
                throw new AssertionException($"No predefined test string for '{appCulture}'.");
            if (this.Application.Settings.GetValueOrDefault(SettingKeys.Culture) != appCulture)
            {
                isTestStringChanged = false;
                latestNotifiedTestString = null;
                this.Application.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, appCulture);
            }
            await Task.Delay(1000, cancellationToken);
            Assert.That(isTestStringChanged, $"Did not receive change notification after changing culture to '{appCulture}'.");
            Assert.That(expectedString == latestNotifiedTestString, "String from observer is incorrect");
            Assert.That(expectedString == this.Application.GetString("LocalizationTest.String"), "String is incorrect");
            var appCultureInfo = await appCulture.GetCultureInfoAsync();
            var currentCulture = CultureInfo.CurrentCulture;
            var currentUICulture = CultureInfo.CurrentUICulture;
            var taskCompletionSource = new TaskCompletionSource();
            Dispatcher.UIThread.Post(() => // Prevent getting CultureInfo.Current(UI)Culture in task context
            {
                currentCulture = CultureInfo.CurrentCulture;
                currentUICulture = CultureInfo.CurrentUICulture;
                taskCompletionSource.TrySetResult();
            });
            await taskCompletionSource.Task;
            Assert.That(appCultureInfo.Name == currentCulture.Name, "CultureInfo.CurrentCulture is incorrect");
            Assert.That(appCultureInfo.Name == currentUICulture.Name, "CultureInfo.CurrentUICulture is incorrect");
            Assert.That(appCultureInfo.Name == CultureInfo.DefaultThreadCurrentCulture?.Name, "CultureInfo.DefaultThreadCurrentCulture is incorrect");
            Assert.That(appCultureInfo.Name == CultureInfo.DefaultThreadCurrentUICulture?.Name, "CultureInfo.DefaultThreadCurrentUICulture is incorrect");
            switch (appCulture)
            {
                case ApplicationCulture.ZH_CN:
                    Assert.That(this.Application.ChineseVariant == ChineseVariant.Default, "Variant of Chinese is incorrect");
                    break;
                case ApplicationCulture.ZH_TW:
                    Assert.That(this.Application.ChineseVariant == ChineseVariant.Taiwan, "Variant of Chinese is incorrect");
                    break;
                default:
                {
                    var expectedVariant = (await ApplicationCulture.System.GetCultureInfoAsync(true)).GetChineseVariant();
                    Assert.That(this.Application.ChineseVariant == expectedVariant, "Variant of Chinese is incorrect");
                    break;
                }
            }
        }
    }


    /// <inheritdoc/>
    protected override Task OnSetupAsync()
    {
        this.initAppCulture = this.Application.Settings.GetValueOrDefault(SettingKeys.Culture);
        return Task.CompletedTask;
    }


    /// <inheritdoc/>
    protected override Task OnTearDownAsync()
    {
        this.Application.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, this.initAppCulture);
        return Task.CompletedTask;
    }
}