using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.Converters;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Data.Converters;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Tests
{
    partial class MainWindow : Controls.MainWindow<App, Workspace>
    {
        public MainWindow()
        {
            InitializeComponent();
        }


        ViewModels.ApplicationOptions ApplicationOptions { get; } = new ViewModels.ApplicationOptions();


        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


        protected override void OnApplicationPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnApplicationPropertyChanged(e);
            if (e.PropertyName == nameof(IAppSuiteApplication.IsRestartingMainWindowsNeeded))
            {
                if (this.Application.IsRestartingMainWindowsNeeded)
                    this.Application.RestartMainWindows();
            }
        }


        void SwitchTheme()
        {
            this.Settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, this.Settings.GetValueOrDefault(SettingKeys.ThemeMode) switch
            {
                ThemeMode.System => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.Light,
                _ => ThemeMode.System,
            });
        }


        async void Test()
        {
            var s = Converters.TimeSpanConverter.Default.Convert<string>(new TimeSpan(1, 2, 3, 4, 5));
            s = Converters.TimeSpanConverter.Default.Convert<string>(new TimeSpan(2, 3, 4, 5));
            s = Converters.TimeSpanConverter.Default.Convert<string>(new TimeSpan(3, 4, 5));
            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromSeconds(123.0));
            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromSeconds(123.456));
            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromSeconds(123.456789));
            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromMilliseconds(123));
            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromMilliseconds(123.456));

            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromMilliseconds(-123.456));
            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromMilliseconds(-123));
            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromSeconds(-23.456789));
            s = Converters.TimeSpanConverter.Default.Convert<string>(TimeSpan.FromSeconds(-123.456789));
            s = Converters.TimeSpanConverter.Default.Convert<string>(new TimeSpan(-1, 2, 3, 4, 5));

            s = Converters.TimeSpanConverter.Default.Convert<string>(new TimeSpan());
        }

        void Test2()
        {
            this.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, this.Settings.GetValueOrDefault(SettingKeys.Culture) switch
            {
                ApplicationCulture.System => ApplicationCulture.EN_US,
                ApplicationCulture.EN_US => ApplicationCulture.ZH_TW,
                _ => ApplicationCulture.System,
            });
        }
    }
}
