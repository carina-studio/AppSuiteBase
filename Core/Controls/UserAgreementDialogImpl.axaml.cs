using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CarinaStudio.Threading;
using System;
using System.ComponentModel;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to show user agreement.
    /// </summary>
    partial class UserAgreementDialogImpl : Dialog
    {
        // Static fields.
        static readonly StyledProperty<string?> Message1Property = AvaloniaProperty.Register<UserAgreementDialogImpl, string?>("Message1");
        static readonly StyledProperty<string?> Message2Property = AvaloniaProperty.Register<UserAgreementDialogImpl, string?>("Message2");


        // Fields.
        bool hasResult;


        // Constructor.
        public UserAgreementDialogImpl()
        {
            AvaloniaXamlLoader.Load(this);
            var app = this.Application;
            var appName = app.Name;
            if (app.IsUserAgreementAgreedBefore)
            {
                this.SetValue<string?>(Message1Property, app.GetFormattedString("UserAgreementDialog.Message.Updated.Section1", appName));
                this.SetValue<string?>(Message2Property, app.GetFormattedString("UserAgreementDialog.Message.Updated.Section2", appName));
            }
            else
            {
                this.SetValue<string?>(Message1Property, app.GetFormattedString("UserAgreementDialog.Message.Section1", appName));
                this.SetValue<string?>(Message2Property, app.GetFormattedString("UserAgreementDialog.Message.Section2", appName));
            }
        }


        /// <summary>
        /// Agree the user agreement.
        /// </summary>
        public void Agree()
        {
            this.hasResult = true;
            this.Application.AgreeUserAgreement();
            this.Close(true);
        }


        /// <summary>
        /// Decline the user agreement.
        /// </summary>
        public void Decline()
        {
            this.hasResult = true;
            this.Close(false);
        }


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
                if (this.Application.IsUserAgreementAgreed)
                {
                    appInfo.UserAgreementUri?.Let(uri => Platform.OpenLink(uri));
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
