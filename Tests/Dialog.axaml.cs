using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.Controls;
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

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            var textBlock = this.FindControl<TextBlock>("radioButtonLabel");
            if (textBlock != null)
            {
                //this.ScrollToControl(textBlock);
                //this.AnimateTextBlock(textBlock);
            }
        }

        public void Test()
        {
            using var appInfo = new AppInfo();
            new AppSuite.Controls.ApplicationInfoDialog(appInfo).ShowDialog(this);
        }
    }
}
