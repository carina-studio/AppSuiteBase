using Avalonia.Threading;
using CarinaStudio.Configuration;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Testing.Common;

class LocalizationTest : TestCase
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


    // Constructor.
    public LocalizationTest(IAppSuiteApplication app) : base(app, TestCaseCategoryNames.Common, "Localization")
    { }


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
                latestNotifiedTestString = default;
                this.Application.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, appCulture);
            }
            await Task.Delay(1000, cancellationToken);
            Assert.IsTrue(isTestStringChanged, $"Did not receive change notification after changing culture to '{appCulture}'.");
            Assert.AreEqual(expectedString, latestNotifiedTestString, "String from observer is incorrect");
            Assert.AreEqual(expectedString, this.Application.GetString("LocalizationTest.String"), "String is incorrect");
            var appCultureInfo = await appCulture.ToCultureInfoAsync();
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
            Assert.AreEqual(appCultureInfo.Name, currentCulture.Name, "CultureInfo.CurrentCulture is incorrect");
            Assert.AreEqual(appCultureInfo.Name, currentUICulture.Name, "CultureInfo.CurrentUICulture is incorrect");
            Assert.AreEqual(appCultureInfo.Name, CultureInfo.DefaultThreadCurrentCulture?.Name, "CultureInfo.DefaultThreadCurrentCulture is incorrect");
            Assert.AreEqual(appCultureInfo.Name, CultureInfo.DefaultThreadCurrentUICulture?.Name, "CultureInfo.DefaultThreadCurrentUICulture is incorrect");
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