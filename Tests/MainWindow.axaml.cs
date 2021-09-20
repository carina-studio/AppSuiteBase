using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;

namespace CarinaStudio.AppSuite.Tests
{
    public partial class MainWindow : Window<IAppSuiteApplication>
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


        void Test()
        {
            this.Settings.SetValue<ThemeMode>(this.Application.ThemeModeSettingKey, this.Settings.GetValueOrDefault(this.Application.ThemeModeSettingKey) switch
            {
                ThemeMode.System => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.Light,
                _ => ThemeMode.System,
            });
        }
    }
}
