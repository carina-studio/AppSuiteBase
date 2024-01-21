using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.AppSuite.ViewModels;
using ScrollViewerExtensions = CarinaStudio.Controls.ScrollViewerExtensions;

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
            /*
            var scrollViewer = this.Get<ScrollViewer>("scrollViewer");
            var textBlock = this.Get<TextBlock>("radioButtonLabel");
            ScrollViewerExtensions.ScrollIntoView(scrollViewer, textBlock, true);
            this.AnimateTextBlock(textBlock);
            */
        }

        public void Test()
        {
            if (this.TutorialPresenter is not null)
                _ = new MessageDialog { Icon = MessageDialogIcon.Success, Message = "TutorialPresenter found." }.ShowDialog(this);
            else
                _ = new MessageDialog { Icon = MessageDialogIcon.Error, Message = "TutorialPresenter not found." }.ShowDialog(this);
        }
    }
}
