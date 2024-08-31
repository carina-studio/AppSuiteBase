using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using System;
using System.Reflection;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="Avalonia.Controls.DatePicker"/> which supports changing content according to current culture.
    /// </summary>
    public class DatePicker : Avalonia.Controls.DatePicker
    {
        // Static fields.
        static readonly MethodInfo? setGridMethod = typeof(Avalonia.Controls.DatePicker).GetMethod("SetGrid", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly MethodInfo? setDateTextMethod = typeof(Avalonia.Controls.DatePicker).GetMethod("SetSelectedDateText", BindingFlags.Instance | BindingFlags.NonPublic);


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            // call base.
            base.OnApplyTemplate(e);

            // get application
            var app = Application.CurrentOrNull;

            // setup day text
            e.NameScope.Find<TextBlock>("DayText")?.Also(it =>
            {
                it.Text = app?.GetString("DatePicker.Day");
                it.VerticalAlignment = VerticalAlignment.Center;
                it.PropertyChanged += (_, e) =>
                {
                    if (e.Property == TextBlock.TextProperty && (e.NewValue as string) == "day")
                    {
                        var text = app?.GetString("DatePicker.Day");
                        if (text != "day")
                            it.Text = text;
                    }
                };
            });

            // setup month text
            e.NameScope.Find<TextBlock>("MonthText")?.Also(it =>
            {
                it.Text = app?.GetString("DatePicker.Month");
                it.PropertyChanged += (_, e) =>
                {
                    if (e.Property == TextBlock.TextProperty && (e.NewValue as string) == "month")
                    {
                        var text = app?.GetString("DatePicker.Month");
                        if (text != "day")
                            it.Text = text;
                    }
                };
            });

            // setup year text
            e.NameScope.Find<TextBlock>("YearText")?.Also(it =>
            {
                it.Text = app?.GetString("DatePicker.Year");
                it.PropertyChanged += (_, e) =>
                {
                    if (e.Property == TextBlock.TextProperty && (e.NewValue as string) == "year")
                    {
                        var text = app?.GetString("DatePicker.Year");
                        if (text != "day")
                            it.Text = text;
                    }
                };
            });
        }


        // Application strings updated.
        void OnAppStringsUpdated(object? sender, EventArgs e)
        {
            setGridMethod?.Invoke(this, null);
            setDateTextMethod?.Invoke(this, null);
        }


        /// <inheritdoc/>
        protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            base.OnAttachedToLogicalTree(e);
            Application.CurrentOrNull?.Let(it => it.StringsUpdated += this.OnAppStringsUpdated);
        }


        /// <inheritdoc/>
        protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
        {
            Application.CurrentOrNull?.Let(it => it.StringsUpdated -= this.OnAppStringsUpdated);
            base.OnDetachedFromLogicalTree(e);
        }


        /// <inheritdoc/>
        protected override Type StyleKeyOverride => typeof(Avalonia.Controls.DatePicker);
    }
}
