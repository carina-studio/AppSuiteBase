using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Tests
{
    partial class MainWindow : Controls.MainWindow<App, Workspace>
    {
        public MainWindow()
        {
            InitializeComponent();
        }


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
            using var appInfo = new ViewModels.ApplicationInfo();
            await new ApplicationInfoDialog(appInfo).ShowDialog(this);
        }

        void Test2()
        {
            this.Application.Shutdown();
        }
    }
}
