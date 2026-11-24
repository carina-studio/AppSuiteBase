using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CarinaStudio.AppSuite.Converters;

// Predefined converter for width of TabItem.
static class TabItemWidthConverter
{
    // Fields.
    static IAvaloniaApplication? app;
    static CachedResource<Thickness>? compactEmptyTabItemPadding;
    static CachedResource<Thickness>? compactItemsPanelMargin;
    static CachedResource<double>? compactMaxTabItemWidth;
    static CachedResource<double>? compactMinTabItemWidth;
    static CachedResource<Thickness>? compactSeparatorMargin;
    static CachedResource<Thickness>? compactTabItemMargin;
    static CachedResource<Thickness>? emptyTabItemPadding;
    static CachedResource<Thickness>? itemsPanelMargin;
    static CachedResource<double>? maxTabItemWidth;
    static CachedResource<double>? minTabItemWidth;
    static CachedResource<Thickness>? separatorMargin;
    static CachedResource<Thickness>? tabItemMargin;


    // Default instance.
    public static readonly IMultiValueConverter Default = new FuncMultiValueConverter<object?, double>(values =>
    {
        var resourceHost = app as IResourceHost ?? IAppSuiteApplication.CurrentOrNull?.Let(it =>
        {
            it.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(IAppSuiteApplication.TextScaleFactor))
                {
                    compactEmptyTabItemPadding = null;
                    compactItemsPanelMargin = null;
                    compactMaxTabItemWidth = null;
                    compactMinTabItemWidth = null;
                    compactSeparatorMargin = null;
                    compactTabItemMargin = null;
                    emptyTabItemPadding = null;
                    itemsPanelMargin = null;
                    maxTabItemWidth = null;
                    minTabItemWidth = null;
                    tabItemMargin = null;
                    separatorMargin = null;
                }
            };
            app = it;
            return it as IResourceHost;
        });
        if (resourceHost is null)
            return double.PositiveInfinity;
        var valueList = values as IList<object?> ?? values.ToArray();
        if (valueList.Count >= 2
            && valueList[0] is TabControl tabControl
            && valueList[1] is Controls.TabStripScrollViewer tabStrip
            && tabStrip.Content is Control tabItemsContainer)
        {
            // select resources by role of tab control
            CachedResource<Thickness> emptyTabItemPadding;
            CachedResource<Thickness> itemsPanelMargin;
            CachedResource<double> maxTabItemWidth;
            CachedResource<double> minTabItemWidth;
            CachedResource<Thickness> separatorMargin;
            CachedResource<Thickness> tabItemMargin;
            if (tabControl is Controls.TabControl { Role: Controls.TabControlRole.Compact })
            {
                compactEmptyTabItemPadding ??= new(resourceHost, "Thickness/TabItem.Header.Padding.Compact");
                compactItemsPanelMargin ??= new(resourceHost, "Thickness/TabControl.TabStrip.Panel.Margin.Compact");
                compactMaxTabItemWidth ??= new(resourceHost, "Double/TabItem.Header.MaxWidth.Compact");
                compactMinTabItemWidth ??= new(resourceHost, "Double/TabItem.Header.MinWidth.Compact");
                compactSeparatorMargin ??= new(resourceHost, "Thickness/TabItem.Header.Separator.Margin.Compact");
                compactTabItemMargin ??= new(resourceHost, "Thickness/TabItem.Header.Margin.Compact");
                emptyTabItemPadding = compactEmptyTabItemPadding;
                itemsPanelMargin = compactItemsPanelMargin;
                maxTabItemWidth = compactMaxTabItemWidth;
                minTabItemWidth = compactMinTabItemWidth;
                separatorMargin = compactSeparatorMargin;
                tabItemMargin = compactTabItemMargin;
            }
            else
            {
                TabItemWidthConverter.emptyTabItemPadding ??= new(resourceHost, "Thickness/TabItem.Header.Padding");
                TabItemWidthConverter.itemsPanelMargin ??= new(resourceHost, "Thickness/TabControl.TabStrip.Panel.Margin");
                TabItemWidthConverter.maxTabItemWidth ??= new(resourceHost, "Double/TabItem.Header.MaxWidth");
                TabItemWidthConverter.minTabItemWidth ??= new(resourceHost, "Double/TabItem.Header.MinWidth");
                TabItemWidthConverter.separatorMargin ??= new(resourceHost, "Thickness/TabItem.Header.Separator.Margin");
                TabItemWidthConverter.tabItemMargin ??= new(resourceHost, "Thickness/TabItem.Header.Margin");
                emptyTabItemPadding = TabItemWidthConverter.emptyTabItemPadding;
                itemsPanelMargin = TabItemWidthConverter.itemsPanelMargin;
                maxTabItemWidth = TabItemWidthConverter.maxTabItemWidth;
                minTabItemWidth = TabItemWidthConverter.minTabItemWidth;
                separatorMargin = TabItemWidthConverter.separatorMargin;
                tabItemMargin = TabItemWidthConverter.tabItemMargin;
            }

            // get size of tab strip
            var margin = tabItemsContainer.Margin;
            var padding = tabStrip.Padding;
            var tabStripWidth = (tabStrip.Bounds.Width 
                                 - margin.Left - padding.Left - itemsPanelMargin.Value.Left
                                 - padding.Right - margin.Right - itemsPanelMargin.Value.Right
                                 - 1);
            if (tabStripWidth <= 0)
                return 0;
            
            // collect tab items
            var tabItemCount = tabControl.Items.Let(items =>
            {
                var count = 0;
                foreach (var item in items)
                {
                    if (item is TabItem tabItem)
                    {
                        if (tabItem.Classes.Contains("Empty") || tabItem.Classes.Contains("EmptyWithSeparator"))
                        {
                            if (tabItem.Header is Control emptyTabItemHeader && emptyTabItemHeader.IsMeasureValid)
                            {
                                var margin = emptyTabItemHeader.Margin;
                                tabStripWidth -= (emptyTabItemHeader.Bounds.Width + emptyTabItemPadding.Value.Left + margin.Left + margin.Right + emptyTabItemPadding.Value.Right);
                            }
                        }
                        else
                            ++count;
                    }
                }
                return count;
            });
            if (tabItemCount <= 0)
                return double.PositiveInfinity;

            // calculate width
            var width = (int)(tabStripWidth / tabItemCount - tabItemMargin.Value.Left - tabItemMargin.Value.Right - separatorMargin.Value.Left - separatorMargin.Value.Right - 1 /* Width of separator */ - 0.5);
            return Math.Max(Math.Min(width, maxTabItemWidth.Value), minTabItemWidth.Value);
        }
        return double.PositiveInfinity;
    });
}
