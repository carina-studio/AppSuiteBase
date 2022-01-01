using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="Avalonia.Controls.TabControl"/> with UX improvement.
    /// </summary>
    public class TabControl : Avalonia.Controls.TabControl, IStyleable
    {
        // Fields.
        readonly ScheduledAction scrollToSelectedItemAction;
        ItemsPresenter? tabItemsPresenter;
        TabStripScrollViewer? tabStripScrollViewer;


        /// <summary>
        /// Initialize new <see cref="TabControl"/> instance.
        /// </summary>
        public TabControl()
        {
            this.scrollToSelectedItemAction = new ScheduledAction(() =>
            {
                // get state
                var index = this.SelectedIndex;
                if (index < 0)
                    return;
                if (this.tabStripScrollViewer == null || this.tabItemsPresenter == null)
                    return;

                // find tab item header
                var tabItemHeader = this.tabItemsPresenter.FindDescendantOfType<Panel>()?.Let(panel =>
                {
                    if (panel.Children.Count > index)
                        return panel.Children[index];
                    return null;
                });
                if (tabItemHeader == null)
                    return;

                // scroll into viewport
                var tabItemHeaderBounds = tabItemHeader.Bounds;
                var left = tabItemHeaderBounds.Left + this.tabItemsPresenter.Bounds.Left;
                var parent = this.tabItemsPresenter.Parent;
                while (parent != this.tabStripScrollViewer && parent != null)
                {
                    left += parent.Bounds.Left;
                    parent = parent.Parent;
                }
                if (left < 0)
                    this.tabStripScrollViewer.ScrollBy(left);
                else if (left + tabItemHeaderBounds.Width > this.tabStripScrollViewer.Bounds.Width)
                    this.tabStripScrollViewer.ScrollBy(left + tabItemHeaderBounds.Width - this.tabStripScrollViewer.Bounds.Width);
            });
        }


        /// <inheritdoc/>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            this.tabItemsPresenter = e.NameScope.Find<ItemsPresenter>("PART_ItemsPresenter");
            this.tabStripScrollViewer = e.NameScope.Find<TabStripScrollViewer>("PART_TabStripScrollViewer");
        }


        /// <inheritdoc/>
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SelectedIndexProperty)
                this.scrollToSelectedItemAction.Schedule();
        }


        // Interface implementation.
        Type IStyleable.StyleKey { get; } = typeof(Avalonia.Controls.TabControl);
    }
}
