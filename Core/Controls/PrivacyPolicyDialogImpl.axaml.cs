using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to show Privacy Policy.
    /// </summary>
    partial class PrivacyPolicyDialogImpl : Dialog
    {
        // Static fields.
        static readonly AvaloniaProperty<string?> Message1Property = AvaloniaProperty.Register<PrivacyPolicyDialogImpl, string?>("Message1");
        static readonly AvaloniaProperty<string?> Message2Property = AvaloniaProperty.Register<PrivacyPolicyDialogImpl, string?>("Message2");


        // Fields.
        bool hasResult;


        // Constructor.
        public PrivacyPolicyDialogImpl()
        {
            InitializeComponent();
            var app = this.Application;
            var appName = app.Name;
            if (app.IsPrivacyPolicyAgreedBefore)
            {
                this.SetValue<string?>(Message1Property, app.GetFormattedString("PrivacyPolicyDialog.Message.Updated.Section1", appName));
                this.SetValue<string?>(Message2Property, app.GetFormattedString("PrivacyPolicyDialog.Message.Updated.Section2", appName));
            }
            else
            {
                this.SetValue<string?>(Message1Property, app.GetFormattedString("PrivacyPolicyDialog.Message.Section1", appName));
                this.SetValue<string?>(Message2Property, app.GetFormattedString("PrivacyPolicyDialog.Message.Section2", appName));
            }
        }


        // Agree the user agreement.
        void Agree()
        {
            this.hasResult = true;
            this.Application.AgreePrivacyPolicy();
            this.Close(true);
        }


        // Agree the user agreement.
        void Decline()
        {
            this.hasResult = true;
            this.Close(false);
        }


        // Initialize.
        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


        // Called when closing.
        protected override void OnClosing(CancelEventArgs e)
        {
            if (!this.hasResult)
                e.Cancel = true;
            base.OnClosing(e);
        }


        // Window opened.
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (this.DataContext is ViewModels.ApplicationInfo appInfo)
            {
                if (this.Application.IsPrivacyPolicyAgreed)
                {
                    appInfo.PrivacyPolicyUri?.Let(uri => Platform.OpenLink(uri));
                    this.SynchronizationContext.Post(this.Agree);
                }
                else
                    this.FindControl<Button>("agreeButton")?.Focus();
            }
            else
                this.SynchronizationContext.Post(this.Decline);
        }
    }
}
