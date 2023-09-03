using Avalonia.Data.Converters;
using CarinaStudio.AppSuite.Scripting;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// View-model for application options UI.
    /// </summary>
    public class ApplicationOptions : ViewModel<IAppSuiteApplication>
    {
        /// <summary>
        /// <see cref="IValueConverter"/> to convert from <see cref="ApplicationCulture"/> to <see cref="string"/>.
        /// </summary>
        public static readonly IValueConverter ApplicationCultureConverter = new Converters.EnumConverter(AppSuiteApplication.CurrentOrNull, typeof(ApplicationCulture));
        /// <summary>
        /// <see cref="IValueConverter"/> to convert from <see cref="ThemeMode"/> to <see cref="string"/>.
        /// </summary>
        public static readonly IValueConverter ThemeModeConverter = new Converters.EnumConverter(AppSuiteApplication.CurrentOrNull, typeof(ThemeMode));
        
        
        // Static fields.
        static bool? InitUseEmbeddedFontsForChinese;


        // Fields.
        readonly ThemeMode originalThemeMode;
        readonly bool originalUsingCompactUI;


        /// <summary>
        /// Initialize new <see cref="ApplicationOptions"/> instance.
        /// </summary>
        public ApplicationOptions() : base(IAppSuiteApplication.Current)
        {
            this.HasMainWindows = this.Application.MainWindows.IsNotEmpty();
            this.IsCustomScreenScaleFactorSupported = double.IsFinite(this.Application.CustomScreenScaleFactor);
            this.IsCustomScreenScaleFactorAdjusted = this.IsCustomScreenScaleFactorSupported
                && Math.Abs(this.Application.CustomScreenScaleFactor - this.Application.EffectiveCustomScreenScaleFactor) >= 0.01;
            this.ThemeModes = new List<ThemeMode>(Enum.GetValues<ThemeMode>()).Also(it =>
            {
                if (!this.Application.IsSystemThemeModeSupported)
                    it.Remove(ThemeMode.System);
            }).AsReadOnly();
            this.originalThemeMode = this.ThemeMode;
            this.originalUsingCompactUI = this.UseCompactUserInterface;
            (this.Application as AppSuiteApplication)?.InitSettings.Let(initSettings =>
            {
                initSettings.SettingChanged += this.OnInitSettingsChanged;
                InitUseEmbeddedFontsForChinese ??= initSettings.GetValueOrDefault(InitSettingKeys.UseEmbeddedFontsForChinese);
                this.IsUseEmbeddedFontsForChineseSupported = true;
                this.IsUseEmbeddedFontsForChineseChanged = InitUseEmbeddedFontsForChinese != initSettings.GetValueOrDefault(InitSettingKeys.UseEmbeddedFontsForChinese);
            });
            ((INotifyCollectionChanged)this.Application.MainWindows).CollectionChanged += this.OnMainWindowsChanged;
            this.Application.ProductManager.Let(it =>
            {
                if (!it.IsMock)
                {
                    if (this.Application is AppSuiteApplication asApp && asApp.ProVersionProductId != null)
                        this.IsProVersionActivated = it.IsProductActivated(asApp.ProVersionProductId);
                    it.ProductActivationChanged += this.OnProductActivationStateChanged;
                }
            });
            this.CheckXRandRAsync();
        }


        /// <summary>
        /// Get or set whether to accept application update with non-stable version or not.
        /// </summary>
        public bool AcceptNonStableApplicationUpdate
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.AcceptNonStableApplicationUpdate);
            set => this.Settings.SetValue<bool>(SettingKeys.AcceptNonStableApplicationUpdate, value);
        }


        // Check installation of XRandR.
        async void CheckXRandRAsync()
        {
            // check platform
            if (Platform.IsNotLinux)
            {
                this.IsXRandRInstalled = false;
                this.OnPropertyChanged(nameof(IsXRandRInstalled));
                return;
            }

            // update state
            this.IsCheckingXRandR = true;
            this.OnPropertyChanged(nameof(IsCheckingXRandR));

            // check built-in XRandR
            var xRandRPath = Path.Combine(this.Application.RootPrivateDirectoryPath, "XRandR", RuntimeInformation.ProcessArchitecture.ToString().ToLower(), "xrandr");
            var isXRandRInstalled = await Task.Run(() =>
                Global.RunOrDefault(() => File.Exists(xRandRPath)));
            
            // check XRandR installed on system
            if (!isXRandRInstalled)
            {
                isXRandRInstalled = await Task.Run(() =>
                {
                    try
                    {
                        using var process = Process.Start(new ProcessStartInfo()
                        {
                            CreateNoWindow = true,
                            FileName = "xrandr",
                            RedirectStandardError = true,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                        });
                        if (process == null)
                            return false;
                        process.WaitForExit(3000);
                        Global.RunWithoutError(() =>
                        {
                            if (!process.HasExited)
                                process.Kill();
                        });
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
                });
            }

            // complete
            if (this.IsXRandRInstalled != isXRandRInstalled)
            {
                this.IsXRandRInstalled = isXRandRInstalled;
                this.OnPropertyChanged(nameof(IsXRandRInstalled));
            }
            this.IsCheckingXRandR = false;
            this.OnPropertyChanged(nameof(IsCheckingXRandR));
        }


        /// <summary>
        /// Get or set application culture.
        /// </summary>
        public ApplicationCulture Culture
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.Culture);
            set => this.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, value);
        }


        /// <summary>
        /// Get available values of <see cref="Culture"/>.
        /// </summary>
        public IList<ApplicationCulture> Cultures { get; } = new List<ApplicationCulture>(Enum.GetValues<ApplicationCulture>()).AsReadOnly();


        /// <summary>
        /// Get or set custom screen scale factor.
        /// </summary>
        public double CustomScreenScaleFactor
        {
            get 
            {
                var factor = this.Application.CustomScreenScaleFactor;
                if (!double.IsFinite(factor) || factor < 1.0)
                    return 1.0;
                if (factor > this.MaxCustomScreenScaleFactor)
                    return this.MaxCustomScreenScaleFactor;
                return this.CustomScreenScaleFactorGranularity * (int)((factor / this.CustomScreenScaleFactorGranularity) + 0.5);
            }
            set
            {
                if (!double.IsFinite(value))
                    return;
                if (value < 1.0)
                    value = 1.0;
                else if (value > this.MaxCustomScreenScaleFactor)
                    value = this.MaxCustomScreenScaleFactor;
                this.Application.CustomScreenScaleFactor = this.CustomScreenScaleFactorGranularity * (int)((value / this.CustomScreenScaleFactorGranularity) + 0.5);
            }
        }


        /// <summary>
        /// Get granularity of <see cref="CustomScreenScaleFactor"/>.
        /// </summary>
        public virtual double CustomScreenScaleFactorGranularity => 0.25;


        /// <summary>
        /// Get or set default language of script.
        /// </summary>
        public ScriptLanguage DefaultScriptLanguage
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.DefaultScriptLanguage);
            set => this.Settings.SetValue<ScriptLanguage>(SettingKeys.DefaultScriptLanguage, value);
        }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            (this.Application as AppSuiteApplication)?.InitSettings.Let(it => it.SettingChanged -= this.OnInitSettingsChanged);
            ((INotifyCollectionChanged)this.Application.MainWindows).CollectionChanged -= this.OnMainWindowsChanged;
            this.Application.ProductManager.Let(it =>
            {
                if (!it.IsMock)
                    it.ProductActivationChanged -= this.OnProductActivationStateChanged;
            });
            base.Dispose(disposing);
        }


        /// <summary>
        /// Get effective custom screen scale factor.
        /// </summary>
        public double EffectiveCustomScreenScaleFactor => this.Application.EffectiveCustomScreenScaleFactor;


        /// <summary>
        /// Get or set whether to enable blurry window background if available or not.
        /// </summary>
        public bool EnableBlurryBackground
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.EnableBlurryBackground);
            set => this.Settings.SetValue<bool>(SettingKeys.EnableBlurryBackground, value);
        }


        /// <summary>
        /// Get or set whether script running is enabled or not.
        /// </summary>
        public bool EnableRunningScript
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.EnableRunningScript);
            set => this.Settings.SetValue<bool>(SettingKeys.EnableRunningScript, value);
        }


        /// <summary>
        /// Check whether at least one main window is valid or not.
        /// </summary>
        public bool HasMainWindows { get; private set; }


        /// <summary>
        /// Get or set indentation size in source code of script.
        /// </summary>
        public int IndentationSizeInScript
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.IndentationSizeInScript);
            set => this.Settings.SetValue<int>(SettingKeys.IndentationSizeInScript, value);
        }


        /// <summary>
        /// Check whether installation of XRandR is being checked or not.
        /// </summary>
        public bool IsCheckingXRandR { get; private set; }


        /// <summary>
        /// Check whether custom screen scale factor is different from effective scale factor or not.
        /// </summary>
        public bool IsCustomScreenScaleFactorAdjusted { get; private set; }


        /// <summary>
        /// Check whether custom screen scale factor is supported or not.
        /// </summary>
        public bool IsCustomScreenScaleFactorSupported { get; }


        /// <summary>
        /// Check whether <see cref="LogOutputTargetPort"/> is supported or not.
        /// </summary>
        public bool IsLogOutputTargetPortSupported => (this.Application as AppSuiteApplication)?.DefaultLogOutputTargetPort != 0;


        /// <summary>
        /// Check whether Pro-version has been activated or not.
        /// </summary>
        public bool IsProVersionActivated { get; private set; }


        /// <summary>
        /// Check whether restarting all root windows is needed or not.
        /// </summary>
        public bool IsRestartingRootWindowsNeeded => this.Application.IsRestartingRootWindowsNeeded;


        /// <summary>
        /// Check whether <see cref="ThemeMode"/> has been changed before restarting main windows or not.
        /// </summary>
        public bool IsThemeModeChanged { get; private set;}


        /// <summary>
        /// Check whether <see cref="UseCompactUserInterface"/> has been changed before restarting main windows or not.
        /// </summary>
        public bool IsUseCompactUserInterfaceChanged { get; private set;}
        
        
        /// <summary>
        /// Check whether <see cref="UseEmbeddedFontsForChinese"/> has been changed before restarting main windows or not.
        /// </summary>
        public bool IsUseEmbeddedFontsForChineseChanged { get; private set; }
        
        
        /// <summary>
        /// Check whether <see cref="UseEmbeddedFontsForChinese"/> is supported or not.
        /// </summary>
        public bool IsUseEmbeddedFontsForChineseSupported { get; private set; }


        /// <summary>
        /// Check whether XRandR tool is installed or not.
        /// </summary>
        public bool IsXRandRInstalled { get; private set; } = true;


        /// <summary>
        /// Whether splash window should be shown when launching application or not.
        /// </summary>
        public bool LaunchWithSplashWindow
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.LaunchWithSplashWindow);
            set => this.Settings.SetValue<bool>(SettingKeys.LaunchWithSplashWindow, value);
        }


        /// <summary>
        /// Get or set port of localhost to receive log output.
        /// </summary>
        public int LogOutputTargetPort
        {
            get => (this.Application as AppSuiteApplication)?.LogOutputTargetPort ?? 0;
            set => (this.Application as AppSuiteApplication)?.Let(app => app.LogOutputTargetPort = value);
        }


        /// <summary>
        /// Get maximum value of <see cref="CustomScreenScaleFactor"/>.
        /// </summary>
        public virtual double MaxCustomScreenScaleFactor => 4.0;


        /// <summary>
        /// Get or set whether to notify user when application update found or not.
        /// </summary>
        public bool NotifyApplicationUpdate
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.NotifyApplicationUpdate);
            set => this.Settings.SetValue<bool>(SettingKeys.NotifyApplicationUpdate, value);
        }


        /// <summary>
        /// Called when property of application changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnApplicationPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnApplicationPropertyChanged(e);
            if (e.PropertyName == nameof(IAppSuiteApplication.CustomScreenScaleFactor))
            {
                this.OnPropertyChanged(nameof(CustomScreenScaleFactor));
                var adjusted = Math.Abs(this.Application.CustomScreenScaleFactor - this.Application.EffectiveCustomScreenScaleFactor) >= 0.01;
                if (this.IsCustomScreenScaleFactorAdjusted != adjusted)
                {
                    this.IsCustomScreenScaleFactorAdjusted = adjusted;
                    this.OnPropertyChanged(nameof(IsCustomScreenScaleFactorAdjusted));
                }
            }
            else if (e.PropertyName == nameof(IAppSuiteApplication.IsRestartingRootWindowsNeeded))
                this.OnPropertyChanged(nameof(IsRestartingRootWindowsNeeded));
            else if (e.PropertyName == nameof(AppSuiteApplication.LogOutputTargetPort))
                this.OnPropertyChanged(nameof(LogOutputTargetPort));
        }
        
        
        // Called when initial settings changed.
        void OnInitSettingsChanged(object? sender, SettingChangedEventArgs e)
        {
            var key = e.Key;
            if (key == InitSettingKeys.UseEmbeddedFontsForChinese)
            {
                this.OnPropertyChanged(nameof(UseEmbeddedFontsForChinese));
                var isChanged = InitUseEmbeddedFontsForChinese.GetValueOrDefault() != (bool)e.Value;
                if (this.IsUseEmbeddedFontsForChineseChanged != isChanged)
                {
                    this.IsUseEmbeddedFontsForChineseChanged = isChanged;
                    this.OnPropertyChanged(nameof(IsUseEmbeddedFontsForChineseChanged));
                }
            }
        }


        // Called when list of main window has been changed.
        void OnMainWindowsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.HasMainWindows != this.Application.MainWindows.IsNotEmpty())
            {
                this.HasMainWindows = !this.HasMainWindows;
                this.OnPropertyChanged(nameof(HasMainWindows));
            }
        }


        // Called when product activation state changed.
        void OnProductActivationStateChanged(Product.IProductManager productManager, string productId, bool isActivated)
        {
            if (!productManager.IsMock && this.Application is AppSuiteApplication asApp && asApp.ProVersionProductId == productId)
            {
                this.IsProVersionActivated = isActivated;
                this.OnPropertyChanged(nameof(IsProVersionActivated));
                this.OnProVersionActivationStateChanged(isActivated);
            }
        }


        /// <summary>
        /// Called when activation of Pro-version has been changed.
        /// </summary>
        /// <param name="isActivated">True if Pro-version is activated.</param>
        protected virtual void OnProVersionActivationStateChanged(bool isActivated)
        { }


        /// <summary>
        /// Called when setting changed.
        /// </summary>
        /// <param name="e">Event data.</param>
        protected override void OnSettingChanged(SettingChangedEventArgs e)
        {
            base.OnSettingChanged(e);
            var key = e.Key;
            if (key == SettingKeys.AcceptNonStableApplicationUpdate)
                this.OnPropertyChanged(nameof(AcceptNonStableApplicationUpdate));
            else if (key == SettingKeys.Culture)
                this.OnPropertyChanged(nameof(Culture));
            else if (key == SettingKeys.DefaultScriptLanguage)
                this.OnPropertyChanged(nameof(DefaultScriptLanguage));
            else if (key == SettingKeys.EnableBlurryBackground)
                this.OnPropertyChanged(nameof(EnableBlurryBackground));
            else if (key == SettingKeys.EnableRunningScript)
                this.OnPropertyChanged(nameof(EnableRunningScript));
            else if (key == SettingKeys.IndentationSizeInScript)
                this.OnPropertyChanged(nameof(IndentationSizeInScript));
            else if (key == SettingKeys.LaunchWithSplashWindow)
                this.OnPropertyChanged(nameof(LaunchWithSplashWindow));
            else if (key == SettingKeys.NotifyApplicationUpdate)
                this.OnPropertyChanged(nameof(NotifyApplicationUpdate));
            else if (key == SettingKeys.PromptWhenScriptRuntimeErrorOccurred)
                this.OnPropertyChanged(nameof(PromptWhenScriptRuntimeErrorOccurred));
            else if (key == SettingKeys.ThemeMode)
            {
                this.OnPropertyChanged(nameof(ThemeMode));
                var changed = (this.originalThemeMode != (ThemeMode)e.Value);
                if (this.IsThemeModeChanged != changed)
                {
                    this.IsThemeModeChanged = changed;
                    this.OnPropertyChanged(nameof(IsThemeModeChanged));
                }
            }
            else if (key == SettingKeys.UseCompactUserInterface)
            {
                this.OnPropertyChanged(nameof(UseCompactUserInterface));
                var changed = (this.originalUsingCompactUI != (bool)e.Value);
                if (this.IsUseCompactUserInterfaceChanged != changed)
                {
                    this.IsUseCompactUserInterfaceChanged = changed;
                    this.OnPropertyChanged(nameof(IsUseCompactUserInterfaceChanged));
                }
            }
            else if (key == SettingKeys.UseSpacesForIndentationInScript)
                this.OnPropertyChanged(nameof(UseSpacesForIndentationInScript));
        }


        /// <summary>
        /// Get or set whether notification dialog should be shown when runtime error occurred by script or not.
        /// </summary>
        public bool PromptWhenScriptRuntimeErrorOccurred
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.PromptWhenScriptRuntimeErrorOccurred);
            set => this.Settings.SetValue<bool>(SettingKeys.PromptWhenScriptRuntimeErrorOccurred, value);
        }


        /// <summary>
        /// Get or set theme mode.
        /// </summary>
        public ThemeMode ThemeMode
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.ThemeMode);
            set
            {
                if (value == ThemeMode.System && !this.Application.IsSystemThemeModeSupported)
                    return;
                this.Settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, value);
            }
        }


        /// <summary>
        /// Get available values of <see cref="ThemeMode"/>.
        /// </summary>
        public IList<ThemeMode> ThemeModes { get; } 


        /// <summary>
        /// Get or set to use compact layout for user interface.
        /// </summary>
        public bool UseCompactUserInterface
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.UseCompactUserInterface);
            set => this.Settings.SetValue<bool>(SettingKeys.UseCompactUserInterface, value);
        }
        
        
        /// <summary>
        /// Get or set to use embedded fonts for Chinese.
        /// </summary>
        public bool UseEmbeddedFontsForChinese
        {
            get => (this.Application as AppSuiteApplication)?.InitSettings.GetValueOrDefault(InitSettingKeys.UseEmbeddedFontsForChinese) ?? false;
            set => (this.Application as AppSuiteApplication)?.InitSettings.Let(it => it.SetValue<bool>(InitSettingKeys.UseEmbeddedFontsForChinese, value));
        }


        /// <summary>
        /// Get or set whether using spaces in source code of script or not.
        /// </summary>
        public bool UseSpacesForIndentationInScript
        {
            get => this.Settings.GetValueOrDefault(SettingKeys.UseSpacesForIndentationInScript);
            set => this.Settings.SetValue<bool>(SettingKeys.UseSpacesForIndentationInScript, value);
        }
    }
}
