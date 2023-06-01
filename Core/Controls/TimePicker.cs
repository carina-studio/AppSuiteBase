using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using System;
using System.Reflection;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="Avalonia.Controls.TimePicker"/> which supports changing content according to current culture.
    /// </summary>
    public class TimePicker : Avalonia.Controls.TimePicker
    {
        // Static fields.
        static readonly MethodInfo? setTimeTextMethod = typeof(Avalonia.Controls.TimePicker).GetMethod("SetSelectedTimeText", BindingFlags.Instance | BindingFlags.NonPublic);


        // Fields.
        TextBlock? hourTextBlock;
        TextBlock? minuteTextBlock;


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            // call base.
            base.OnApplyTemplate(e);

            // setup minute text
            this.minuteTextBlock = e.NameScope.Find<TextBlock>("MinuteTextBlock")?.Also(it =>
            {
                it.PropertyChanged += (_, e) =>
                {
                    if (e.Property == TextBlock.TextProperty)
                        this.OverrideMinuteText();
                };
            });

            // setup hour text
            this.hourTextBlock = e.NameScope.Find<TextBlock>("HourTextBlock")?.Also(it =>
            {
                it.PropertyChanged += (_, e) =>
                {
                    if (e.Property == TextBlock.TextProperty)
                        this.OverrideHourText();
                };
            });

            // setup initial text
            this.OverrideHourText();
            this.OverrideMinuteText();
        }


        // Application strings updated.
        void OnAppStringsUpdated(object? sender, EventArgs e)
        {
            setTimeTextMethod?.Invoke(this, null);
            this.OverrideHourText();
            this.OverrideMinuteText();
        }


        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            AppSuiteApplication.CurrentOrNull?.Let(it => it.StringsUpdated += this.OnAppStringsUpdated);
        }


        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            AppSuiteApplication.CurrentOrNull?.Let(it => it.StringsUpdated -= this.OnAppStringsUpdated);
            base.OnDetachedFromLogicalTree(e);
        }


        // Show hour of selected time.
        void OverrideHourText()
        {
            var app = AppSuiteApplication.CurrentOrNull;
            this.hourTextBlock?.Let(textBlock =>
            {
                textBlock.Text = this.SelectedTime?.Let(it =>
                {
                    var hours = it.Hours;
                    if (!this.ClockIdentifier.StartsWith("24"))
                    {
                        hours %= 12;
                        if (hours == 0)
                            hours = 12;
                    }
                    return app?.GetFormattedString("TimePicker.HourFormat", hours);
                }) ?? app?.GetString("TimePicker.Hour");
            });
        }


        // Show minute of selected time.
        void OverrideMinuteText()
        {
            var app = AppSuiteApplication.CurrentOrNull;
            this.minuteTextBlock?.Let(textBlock =>
            {
                textBlock.Text = this.SelectedTime?.Let(it =>
                {
                    return app?.GetFormattedString("TimePicker.MinuteFormat", it.Minutes);
                }) ?? app?.GetString("TimePicker.Minute");
            });
        }


        /// <inheritdox/>
        protected override Type StyleKeyOverride => typeof(Avalonia.Controls.TimePicker);
    }
}
