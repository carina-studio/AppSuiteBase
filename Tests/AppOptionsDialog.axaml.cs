using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.ViewModels;

namespace CarinaStudio.AppSuite.Tests
{
    public partial class AppOptionsDialog : AppSuite.Controls.BaseApplicationOptionsDialog
    {
        public AppOptionsDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override ApplicationOptions OnCreateViewModel() => new ApplicationOptions();
    }
}
