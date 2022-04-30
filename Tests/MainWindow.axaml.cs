using System.Threading.Tasks;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
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
using CarinaStudio.Windows.Input;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using TabControl = Avalonia.Controls.TabControl;

namespace CarinaStudio.AppSuite.Tests
{
    partial class MainWindow : Controls.MainWindow<App, Workspace>
    {
        const string TabItemKey = "TabItem";


        static readonly AvaloniaProperty<int> Int32Property = AvaloniaProperty.Register<MainWindow, int>("Int32", 1);
         static readonly AvaloniaProperty<IImage?> SelectedImageProperty = AvaloniaProperty.RegisterDirect<MainWindow, IImage?>(nameof(SelectedImage), window => window.selectedImage);
        static readonly AvaloniaProperty<string?> SelectedImageIdProperty = AvaloniaProperty.RegisterDirect<MainWindow, string?>(nameof(SelectedImageId), window => window.selectedImageId);


        readonly MutableObservableBoolean canShowAppInfo = new MutableObservableBoolean(true);
        readonly IntegerTextBox integerTextBox;
        readonly IntegerTextBox integerTextBox2;
        readonly IPAddressTextBox ipAddressTextBox;
        readonly ScheduledAction logAction;
        IImage? selectedImage;
        string? selectedImageId;
        readonly ObservableList<TabItem> tabItems = new();
        readonly TutorialPresenter tutorialPresenter;


        public MainWindow()
        {
            this.ShowAppInfoDialogCommand = new Command(() => this.ShowAppInfoDialog(), this.canShowAppInfo);

            var iconResources = new ResourceInclude().Let(it =>
            {
                it.Source = new Uri("avares://CarinaStudio.AppSuite.Core/Resources/Icons.axaml");
                return it.Loaded;
            });
            this.ImageIdList = iconResources.Keys.Where(it => iconResources[it] is IImage).Cast<string>().ToArray().Also(it => 
            {
                for (var i = it.Length - 1; i >= 0; --i)
                {
                    if (it[i].StartsWith("Image/"))
                        it[i] = it[i].Substring(6);
                }
                Array.Sort(it, string.Compare);
            });

            InitializeComponent();

            this.logAction = new ScheduledAction(() =>
            {
                this.Logger.LogDebug($"Time: {DateTime.Now}");
                this.logAction?.Schedule(500);
            });

            this.integerTextBox = this.FindControl<IntegerTextBox>(nameof(integerTextBox)).AsNonNull();
            this.integerTextBox2 = this.FindControl<IntegerTextBox>(nameof(integerTextBox2)).AsNonNull();
            this.ipAddressTextBox = this.FindControl<IPAddressTextBox>(nameof(ipAddressTextBox)).AsNonNull();

            var tabControl = this.FindControl<TabControl>("tabControl").AsNonNull();
            this.tabItems.AddRange(tabControl.Items.Cast<TabItem>());
            tabControl.Items = this.tabItems;
            (this.tabItems[0].Header as Control)?.Let(it => this.Application.EnsureClosingToolTipIfWindowIsInactive(it));

            this.tutorialPresenter = this.FindControl<TutorialPresenter>(nameof(tutorialPresenter)).AsNonNull();
        }


        ViewModels.ApplicationOptions ApplicationOptions { get; } = new ViewModels.ApplicationOptions();


        void EditConfiguration()
        {
            _ = new SettingsEditorDialog()
            {
                SettingKeys = SettingKey.GetDefinedKeys<ConfigurationKeys>(),
                Settings = this.Configuration,
            }.ShowDialog(this);
        }


        public IList<string> ImageIdList { get; }


        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


        //public override bool IsExtendingClientAreaAllowed => false;


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


        void OnImageIdListBoxSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
            this.SynchronizationContext.Post(() =>
            {
                var imageId = (sender as Avalonia.Controls.ListBox)?.SelectedItem as string;
                this.SetAndRaise<string?>(SelectedImageIdProperty, ref this.selectedImageId, imageId);
                if (imageId != null && this.Application.TryFindResource<IImage>($"Image/{imageId}", out var image) && image != null)
                    this.SetAndRaise<IImage?>(SelectedImageProperty, ref this.selectedImage, image);
                else
                    this.SetAndRaise<IImage?>(SelectedImageProperty, ref this.selectedImage, null);
            });


        protected override void OnInitialDialogsClosed()
        {
            base.OnInitialDialogsClosed();
        }


        void OnListBoxDoubleClickOnItem(object? sender, ListBoxItemEventArgs e)
        {
            _ = new MessageDialog()
            {
                Message = $"Double-clicked on {e.Item} at position {e.ItemIndex}",
            }.ShowDialog(this);
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


        public IImage? SelectedImage { get => this.selectedImage; }


        public string? SelectedImageId { get => this.selectedImageId; }


        async void ShowAppInfoDialog()
        {
            this.canShowAppInfo.Update(false);
            using var appInfo = new AppInfo();
            await new ApplicationInfoDialog(appInfo).ShowDialog(this);
            this.canShowAppInfo.Update(true);
        }

        async void ShowMessageDialog()
        {
            var result = await new MessageDialog()
            {
                Buttons = Enum.GetValues<MessageDialogButtons>().SelectRandomElement(),
                Icon = Enum.GetValues<MessageDialogIcon>().SelectRandomElement(),
                Message = "This is a message dialog!",
            }.ShowDialog(this);
            _ = new MessageDialog()
            {
                Message = $"The result is {result}",
            }.ShowDialog(this);
        }

        ICommand ShowAppInfoDialogCommand { get; }

        async void ShowTestDialog()
        {
            var result = await new Dialog().ShowDialog<ApplicationOptionsDialogResult>(this);
            if (result == ApplicationOptionsDialogResult.RestartMainWindowsNeeded)
                this.Application.RestartMainWindows();
        }


        void ShowTutorial(IVisual anchor)
        {
            var tutorial = new Tutorial().Also(it =>
            {
                it.Anchor = anchor;
                it.Description = "This is a tutorial with long long long long description.";
                it.Dismissed += (_, e) => 
                {
                    ;
                };
                it.Bind(Tutorial.IconProperty, this.GetResourceObservable("Image/Icon.Lightbulb"));
                it.SkippingAllTutorialRequested += (_, e) => 
                {
                    ;
                };
            });
            this.tutorialPresenter.ShowTutorial(tutorial);
        }


        void SwitchAppCulture()
        {
            this.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, this.Settings.GetValueOrDefault(SettingKeys.Culture) switch
            {
                ApplicationCulture.System => ApplicationCulture.EN_US,
                ApplicationCulture.EN_US => ApplicationCulture.ZH_TW,
                _ => ApplicationCulture.System,
            });
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
            //this.integerTextBox2.IsNullValueAllowed = !this.integerTextBox2.IsNullValueAllowed;

            this.integerTextBox.Value = 321;
            this.integerTextBox.Text = "321";
            this.integerTextBox.Value = 1234;
            this.integerTextBox.Text = "1234";

            //this.ipAddressTextBox.Text = "127.0.0.1";
            //this.ipAddressTextBox.IPAddress = System.Net.IPAddress.Parse("127.0.0.1");
            //this.ipAddressTextBox.Text = "192.168.0.1";
            //this.ipAddressTextBox.IPAddress = System.Net.IPAddress.Parse("192.168.0.1");

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
