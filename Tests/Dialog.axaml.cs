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

        protected override ApplicationOptions OnCreateViewModel() => new ApplicationOptions();

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
