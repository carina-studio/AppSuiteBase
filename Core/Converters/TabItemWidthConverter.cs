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
    static Thickness emptyTabItemPadding;
    static Thickness itemsPanelMargin;
    static double maxTabItemWidth;
    static double minTabItemWidth;
    static Thickness tabItemMargin;


    // Default instance.
    public static readonly IMultiValueConverter Default = new FuncMultiValueConverter<object?, double>(values =>
    {
        if (app == null)
        {
            app = AppSuiteApplication.CurrentOrNull;
            if (app == null)
                return double.PositiveInfinity;
            emptyTabItemPadding = app.FindResourceOrDefault<Thickness>("Thickness/TabItem.Header.Padding");
            itemsPanelMargin = app.FindResourceOrDefault<Thickness>("Thickness/TabControl.TabStrip.Panel.Margin");
            maxTabItemWidth = app.FindResourceOrDefault<double>("Double/TabItem.Header.MaxWidth", 300);
            minTabItemWidth = app.FindResourceOrDefault<double>("Double/TabItem.Header.MinWidth", 150);
            tabItemMargin = app.FindResourceOrDefault<Thickness>("Thickness/TabItem.Header.Margin");
        }
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
                                 - margin.Left - padding.Left - itemsPanelMargin.Left
                                 - padding.Right - margin.Right - itemsPanelMargin.Right
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
                            (tabItem.Header as Control)?.Let(it =>
                            {
                                if (it.IsMeasureValid)
                                {
                                    var margin = it.Margin;
                                    tabStripWidth -= (it.Bounds.Width + emptyTabItemPadding.Left + margin.Left + margin.Right + emptyTabItemPadding.Right);
                                }
                            });
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
            var width = (int)(tabStripWidth / tabItemCount - tabItemMargin.Left - tabItemMargin.Right - 0.5);
            return Math.Max(Math.Min(width, maxTabItemWidth), minTabItemWidth);
        }
        return double.PositiveInfinity;
    });
}
