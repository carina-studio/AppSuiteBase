using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.AppSuite.Converters;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Data.Converters;
using CarinaStudio.Input;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using TabControl = Avalonia.Controls.TabControl;

namespace CarinaStudio.AppSuite.Tests
{
    partial class MainWindow : Controls.MainWindow<App, Workspace>
    {
        const string TabItemKey = "TabItem";


        readonly ScheduledAction logAction;
        private readonly ObservableList<TabItem> tabItems = new();


        public MainWindow()
        {
            InitializeComponent();

            this.logAction = new ScheduledAction(() =>
            {
                this.Logger.LogDebug($"Time: {DateTime.Now}");
                this.logAction?.Schedule(500);
            });

            var tabControl = this.FindControl<TabControl>("tabControl").AsNonNull();
            this.tabItems.AddRange(tabControl.Items.Cast<TabItem>());
            tabControl.Items = this.tabItems;
            (this.tabItems[0].Header as Control)?.Let(it => this.Application.EnsureClosingToolTipIfWindowIsInactive(it));
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

            //if (tabItems != null && e.ItemIndex < tabItems.Count - 1)
            //(sender as Controls.TabControl)?.Let(it => it.SelectedIndex = e.ItemIndex);

            if (!e.Data.TryGetData<TabItem>(TabItemKey, out var tabItem) || tabItem == null)
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

            if (!e.Data.TryGetData<TabItem>(TabItemKey, out var item) || item == null || item == tabItem)
                return;

            var srcIndex = tabItems.IndexOf(item);
            if (srcIndex < 0)
                return;

            var targetIndex = e.PointerPosition.X <= e.HeaderVisual.Bounds.Width / 2
                    ? e.ItemIndex
                    : e.ItemIndex + 1;

            if (targetIndex != srcIndex && targetIndex != srcIndex + 1)
            {
                if (srcIndex < targetIndex)
                    this.tabItems.Move(srcIndex, targetIndex - 1);
                else
                    this.tabItems.Move(srcIndex, targetIndex);
            }
        }


        void OnTabItemDragged(object? sender, TabItemDraggedEventArgs e)
        {
            var data = new DataObject();
            data.Set(TabItemKey, e.Item);
            DragDrop.DoDragDrop(e.PointerEventArgs, data, DragDropEffects.Move);
            e.Handled = true;
        }


        void RestartApp()
        {
            this.Application.Restart(AppSuiteApplication.RestoreMainWindowsArgument);
        }


        async void ShowAppInfoDialog()
        {
            using var appInfo = new AppInfo();
            await new ApplicationInfoDialog(appInfo).ShowDialog(this);
        }

        async void ShowTestDialog()
        {
            var result = await new Dialog().ShowDialog<ApplicationOptionsDialogResult>(this);
            if (result == ApplicationOptionsDialogResult.RestartMainWindowsNeeded)
                this.Application.RestartMainWindows();
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


        void Test()
        {
            var sysDecorSizes = ExtendedClientAreaWindowConfiguration.GetSystemDecorationSizes(Screens.ScreenFromVisual(this));
            //this.Settings.SetValue<bool>(SettingKeys.ShowProcessInfo, !this.Settings.GetValueOrDefault(SettingKeys.ShowProcessInfo));
        }

        void Test2()
        {
            this.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, this.Settings.GetValueOrDefault(SettingKeys.Culture) switch
            {
                ApplicationCulture.System => ApplicationCulture.EN_US,
                ApplicationCulture.EN_US => ApplicationCulture.ZH_TW,
                _ => ApplicationCulture.System,
            });
            /*
            this.Application.ShowMainWindow(window =>
            {
                System.Diagnostics.Debug.WriteLine("Window created");
            });
            */
        }
    }
}
