using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using CarinaStudio.AppSuite.Converters;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Data.Converters;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.ComponentModel;
using CarinaStudio.AppSuite.ViewModels;

namespace CarinaStudio.AppSuite.Tests
{
    partial class MainWindow : Controls.MainWindow<App, Workspace>
    {
        const string TabItemKey = "TabItem";


        readonly ScheduledAction logAction;


        public MainWindow()
        {
            InitializeComponent();

            this.logAction = new ScheduledAction(() =>
            {
                this.Logger.LogDebug($"Time: {DateTime.Now}");
                this.logAction?.Schedule(500);
            });
        }


        ViewModels.ApplicationOptions ApplicationOptions { get; } = new ViewModels.ApplicationOptions();


        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


        protected override void OnApplicationPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnApplicationPropertyChanged(e);
        }


        protected override void OnClosed(EventArgs e)
        {
            this.logAction.Cancel();
            base.OnClosed(e);
        }


        protected override ApplicationInfo OnCreateApplicationInfo() => new AppInfo();


        void OnDragEnterTabItem(object? sender, DragOnTabItemEventArgs e)
        {
            (sender as Controls.TabControl)?.ScrollHeaderIntoView(e.ItemIndex);
        }


        void OnDragLeaveTabItem(object? sender, TabItemEventArgs e)
        {
            if (e.Item is not TabItem tabItem)
                return;
            ItemInsertionIndicator.SetInsertingItemAfter(tabItem, false);
            ItemInsertionIndicator.SetInsertingItemBefore(tabItem, false);
        }


        void OnDragOverTabItem(object? sender, DragOnTabItemEventArgs e)
        {
            e.DragEffects = DragDropEffects.None;

            var tabItems = (sender as Controls.TabControl)?.Items as IList;
            if (tabItems == null)
                return;

            //if (tabItems != null && e.ItemIndex < tabItems.Count - 1)
            //(sender as Controls.TabControl)?.Let(it => it.SelectedIndex = e.ItemIndex);

            var tabItem = e.Data.Get(TabItemKey);
            if (tabItem == null)
                return;

            if (tabItem == e.Item)
            {
                e.DragEffects = DragDropEffects.Move;
                e.Handled = true;
            }
            else if (e.ItemIndex < tabItems.Count - 1)
            {
                var srcIndex = tabItems.IndexOf(tabItem);
                if (srcIndex < 0)
                    return;

                e.DragEffects = DragDropEffects.Move;

                var targetIndex = e.PointerPosition.X <= e.HeaderVisual.Bounds.Width / 2
                    ? e.ItemIndex
                    : e.ItemIndex + 1;

                //System.Diagnostics.Debug.WriteLine($"srcIndex: {srcIndex}, targetIndex: {targetIndex}");

                if (targetIndex == srcIndex || targetIndex == srcIndex + 1)
                {
                    ItemInsertionIndicator.SetInsertingItemAfter((Control)e.Item, false);
                    ItemInsertionIndicator.SetInsertingItemBefore((Control)e.Item, false);
                    return;
                }

                if (targetIndex > e.ItemIndex)
                {
                    ItemInsertionIndicator.SetInsertingItemAfter((Control)e.Item, true);
                    ItemInsertionIndicator.SetInsertingItemBefore((Control)e.Item, false);
                }
                else
                {
                    ItemInsertionIndicator.SetInsertingItemAfter((Control)e.Item, false);
                    ItemInsertionIndicator.SetInsertingItemBefore((Control)e.Item, true);
                }

                /*
                var srcIndex = tabItems.IndexOf(tabItem);
                if (srcIndex < 0)
                    return;
                (sender as Controls.TabControl)?.Let(it => it.SelectedIndex = e.ItemIndex);
                tabItems.RemoveAt(srcIndex);
                tabItems.Insert(e.ItemIndex, tabItem);
                (sender as Controls.TabControl)?.Let(it => it.SelectedIndex = e.ItemIndex);
                */
                e.Handled = true;
            }
        }


        void OnDropOnTabItem(object? sender, DragOnTabItemEventArgs e)
        {
            if (e.Item is not TabItem tabItem)
                return;

            ItemInsertionIndicator.SetInsertingItemAfter(tabItem, false);
            ItemInsertionIndicator.SetInsertingItemBefore(tabItem, false);

            var item = e.Data.Get(TabItemKey);
            if (item == null || item == e.Item)
                return;

            var tabItems = (sender as Controls.TabControl)?.Items as IList;
            if (tabItems == null)
                return;

            var srcIndex = tabItems.IndexOf(item);
            if (srcIndex < 0)
                return;

            var targetIndex = e.PointerPosition.X <= e.HeaderVisual.Bounds.Width / 2
                    ? e.ItemIndex
                    : e.ItemIndex + 1;

            if (targetIndex != srcIndex && targetIndex != srcIndex + 1)
            {
                (sender as Controls.TabControl)?.Let(it => it.SelectedIndex = e.ItemIndex);
                tabItems.RemoveAt(srcIndex);
                if (srcIndex < targetIndex)
                {
                    tabItems.Insert(targetIndex - 1, item);
                    (sender as Controls.TabControl)?.Let(it => it.SelectedIndex = targetIndex - 1);
                }
                else
                {
                    tabItems.Insert(targetIndex, item);
                    (sender as Controls.TabControl)?.Let(it => it.SelectedIndex = targetIndex);
                }
            }
        }


        void OnTabItemDragged(object? sender, TabItemDraggedEventArgs e)
        {
            var data = new DataObject();
            data.Set(TabItemKey, e.Item);
            DragDrop.DoDragDrop(e.PointerEventArgs, data, DragDropEffects.Move);
            e.Handled = true;
        }


        void SwitchTheme()
        {
            this.Settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, this.Settings.GetValueOrDefault(SettingKeys.ThemeMode) switch
            {
                ThemeMode.System => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.Light,
                _ => this.Application.IsSystemThemeModeSupported ? ThemeMode.System : ThemeMode.Dark,
            });
        }


        async void Test()
        {
            //using var appInfo = new AppInfo();
            //await new ApplicationInfoDialog(appInfo).ShowDialog(this);
            var result = await new Dialog().ShowDialog<ApplicationOptionsDialogResult>(this);
            if (result == ApplicationOptionsDialogResult.RestartMainWindowsNeeded)
                this.Application.RestartMainWindows();
        }

        void Test2()
        {
            this.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, this.Settings.GetValueOrDefault(SettingKeys.Culture) switch
            {
                ApplicationCulture.System => ApplicationCulture.EN_US,
                ApplicationCulture.EN_US => ApplicationCulture.ZH_TW,
                _ => ApplicationCulture.System,
            });
        }
    }
}
