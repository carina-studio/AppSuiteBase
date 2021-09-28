using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CarinaStudio.AppSuite.Tests
{
    public partial class Dialog : CarinaStudio.Controls.Dialog<IAppSuiteApplication<App>>
    {
        public Dialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
