using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Controls;
using System;
using System.Globalization;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to show application change list.
    /// </summary>
    partial class ApplicationChangeListDialogImpl : Dialog
    {
        // Convert from ApplicationChangeType to IImage.
        class ApplicationChangeTypeConverterImpl : IValueConverter
        {
            // Fields.
            readonly AppSuiteApplication? app = AppSuiteApplication.CurrentOrNull;

            // Convert.
            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (this.app == null)
                    return null;
                if (value is not ApplicationChangeType type)
                    return null;
                if (!typeof(object).IsAssignableFrom(targetType) && !targetType.IsAssignableFrom(typeof(IImage)))
                    return null;
                IImage? image;
                switch (type)
                {
                    case ApplicationChangeType.BehaviorChange:
                        this.app.TryGetResource<IImage>($"Image/Icon.Information.Outline", out image);
                        break;
                    case ApplicationChangeType.BugFixing:
                        this.app.TryGetResource<IImage>($"Image/Icon.Debug.Outline", out image);
                        break;
                    case ApplicationChangeType.Improvement:
                        this.app.TryGetResource<IImage>($"Image/Icon.Update.Outline", out image);
                        break;
                    case ApplicationChangeType.NewFeature:
                        this.app.TryGetResource<IImage>($"Image/Icon.Star.Outline", out image);
                        break;
                    default:
                        this.app.TryGetResource<IImage>($"Image/Icon.Circle.Outline", out image);
                        break;
                }
                return image;
            }

            // Convert back.
            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
        }


        // Static fields.
        public static readonly IValueConverter ApplicationChangeTypeConverter = new ApplicationChangeTypeConverterImpl();
        static readonly StyledProperty<string?> HeaderProperty = AvaloniaProperty.Register<ApplicationChangeListDialogImpl, string?>(nameof(Header));


        // Fields.
        readonly Panel contentPanel;


        // Constructor.
        public ApplicationChangeListDialogImpl()
        {
            AvaloniaXamlLoader.Load(this);
            this.contentPanel = this.Get<Panel>(nameof(contentPanel));
        }


        // Build change list views.
        void BuildChangeListViews()
        {
            // check state
            if (this.IsClosed)
                return;

            // clear current views
            for (var i = this.contentPanel.Children.Count - 1; i > 1; --i)
                this.contentPanel.Children.RemoveAt(i);

            // build views
            if (this.DataContext is ApplicationChangeList appChangeList)
            {
                var changeList = appChangeList.ChangeList;
                for (int i = 0, count = changeList.Count; i < count; ++i)
                {
                    if (i > 0)
                    {
                        this.contentPanel.Children.Add(new Separator().Also(it =>
                        {
                            it.Classes.Add("Dialog_Separator");
                        }));
                    }
                    this.contentPanel.Children.Add(this.DataTemplates[0]!.Build(changeList[i])!.Also(it =>
                    {
                        it.DataContext = changeList[i];
                    }));
                }
            }
        }


        // Header text.
        public string? Header { get => this.GetValue<string?>(HeaderProperty); }


        /// <inheritdoc/>
        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            (this.DataContext as ApplicationChangeList)?.Let(it =>
            {
                this.SetValue<string?>(HeaderProperty, this.Application.GetFormattedString("ApplicationChangeListDialog.Header", this.Application.Name, $"{it.Version.Major}.{it.Version.Minor}"));
                this.BuildChangeListViews();
            });
        }
    }
}
