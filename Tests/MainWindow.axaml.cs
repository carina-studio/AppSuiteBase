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
            this.Settings.SetValue<ApplicationCulture>(this.Application.CultureSettingKey, this.Settings.GetValueOrDefault(this.Application.CultureSettingKey) switch
            {
                ApplicationCulture.System => ApplicationCulture.EN_US,
                ApplicationCulture.EN_US => ApplicationCulture.ZH_TW,
                _ => ApplicationCulture.System,
            });
        }
    }
}
