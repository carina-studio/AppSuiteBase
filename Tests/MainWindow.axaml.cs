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
            var dialog = new MessageDialog()
            {
                DoNotAskAgain = true,
                Icon = MessageDialogIcon.Question,
                Message = "Test message.\nLine #2\nLine #3\nLoooooooooooong message"
            };
            await dialog.ShowDialog(this);

            var doNotAskAgain = dialog.DoNotAskAgain;
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
