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
    static CachedResource<Thickness>? emptyTabItemPadding;
    static CachedResource<Thickness>? itemsPanelMargin;
    static CachedResource<double>? maxTabItemWidth;
    static CachedResource<double>? minTabItemWidth;
    static CachedResource<Thickness>? separatorMargin;
    static CachedResource<Thickness>? tabItemMargin;


    // Default instance.
    public static readonly IMultiValueConverter Default = new FuncMultiValueConverter<object?, double>(values =>
    {
        var resourceHost = app as IResourceHost ?? AppSuiteApplication.CurrentOrNull?.Let(it =>
        {
            app = it;
            return it as IResourceHost;
        });
        if (resourceHost is null)
            return double.PositiveInfinity;
        emptyTabItemPadding ??= new(resourceHost, "Thickness/TabItem.Header.Padding");
        itemsPanelMargin ??= new(resourceHost, "Thickness/TabControl.TabStrip.Panel.Margin");
        maxTabItemWidth ??= new(resourceHost, "Double/TabItem.Header.MaxWidth");
        minTabItemWidth ??= new(resourceHost, "Double/TabItem.Header.MinWidth");
        tabItemMargin ??= new(resourceHost, "Thickness/TabItem.Header.Margin");
        separatorMargin ??= new(resourceHost, "Thickness/TabItem.Header.Separator.Margin");
        var valueList = values as IList<object?> ?? values.ToArray();
        if (valueList.Count >= 2
            && valueList[0] is TabControl tabControl
            && valueList[1] is Controls.TabStripScrollViewer tabStrip
            && tabStrip.Content is Control tabItemsContainer)
        {
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
                        if (tabItem.Classes.Contains("Empty"))
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
