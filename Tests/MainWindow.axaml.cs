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
            this.ExtendClientAreaToDecorationsHint = true;
            
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


        async void Test()
        {
            this.Settings.SetValue<ThemeMode>(this.Application.ThemeModeSettingKey, this.Settings.GetValueOrDefault(this.Application.ThemeModeSettingKey) switch
            {
                ThemeMode.System => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.Light,
                _ => ThemeMode.System,
            });
        }

        void Test2()
        {
            new Dialog().ShowDialog(this);
        }
    }
}
