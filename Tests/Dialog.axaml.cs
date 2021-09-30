using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CarinaStudio.AppSuite.Tests
{
    public partial class Dialog : Controls.Dialog<App>
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
