using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.ViewModels;

namespace CarinaStudio.AppSuite.Tests
{
    public partial class Dialog : Controls.BaseApplicationOptionsDialog
    {
        public Dialog()
        {
            InitializeComponent();
        }

        protected override ApplicationOptions OnCreateViewModel() => new();

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void Test()
        {
            using var appInfo = new AppInfo();
            new AppSuite.Controls.ApplicationInfoDialog(appInfo).ShowDialog(this);
        }
    }
}
