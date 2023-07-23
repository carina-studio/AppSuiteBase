using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.VisualTree;
using CarinaStudio.AppSuite.Controls;
using CarinaStudio.AppSuite.Controls.Highlighting;
using CarinaStudio.AppSuite.Converters;
using CarinaStudio.AppSuite.Diagnostics;
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
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Markup.Xaml.Styling;
using CarinaStudio.AppSuite.IO;
using TabControl = Avalonia.Controls.TabControl;

namespace CarinaStudio.AppSuite.Tests
{
    partial class MainWindow : Controls.MainWindow<App, Workspace>
    {
        const string TabItemKey = "TabItem";


        static readonly StyledProperty<int> Int32Property = AvaloniaProperty.Register<MainWindow, int>("Int32", 1);
        static readonly DirectProperty<MainWindow, IImage?> SelectedImageProperty = AvaloniaProperty.RegisterDirect<MainWindow, IImage?>(nameof(SelectedImage), window => window.selectedImage);
        static readonly DirectProperty<MainWindow, string?> SelectedImageIdProperty = AvaloniaProperty.RegisterDirect<MainWindow, string?>(nameof(SelectedImageId), window => window.selectedImageId);


        readonly MutableObservableBoolean canShowAppInfo = new(true);
        readonly IDisposable hfProcessInfoUpdateToken;
        readonly IntegerTextBox integerTextBox;
        readonly IntegerTextBox integerTextBox2;
        readonly IPAddressTextBox ipAddressTextBox;
        readonly ScheduledAction logAction;
        readonly NotificationPresenter notificationPresenter;
        IDisposable? overlayResourcesToken;
        IImage? selectedImage;
        string? selectedImageId;
        readonly Avalonia.Controls.TextBlock syntaxHighlightingRefTextBlock;
        readonly SyntaxHighlightingTextBlock syntaxHighlightingTextBlock;
        readonly SyntaxHighlightingTextBox syntaxHighlightingTextBox;
        readonly ObservableList<TabItem> tabItems = new();


        public MainWindow()
        {
            this.ShowAppInfoDialogCommand = new Command(() => this.ShowAppInfoDialog(), this.canShowAppInfo);
            this.ShowTutorialCommand = new Command<Visual>(this.ShowTutorial);

            this.hfProcessInfoUpdateToken = this.Application.ProcessInfo.RequestHighFrequencyUpdate();

            var iconResources = new ResourceInclude(new Uri("avares://CarinaStudio.AppSuite.Core")).Let(it =>
            {
                it.Source = new Uri("Resources/Icons.axaml", UriKind.Relative);
                return it.Loaded;
            });
            this.ImageIdList = iconResources.Keys.Where(it => iconResources[it] is IImage).Cast<string>().ToArray().Also(it => 
            {
                for (var i = it.Length - 1; i >= 0; --i)
                {
                    if (it[i].StartsWith("Image/"))
                        it[i] = it[i][6..^0];
                }
                Array.Sort(it, string.Compare);
            });

            this.Application.LoadingStrings += this.OnAppLoadingStrings;
            this.SetupCustomResource(this.Application.CultureInfo);

            this.RegexSyntaxHighlightingDefinitionSet = RegexSyntaxHighlighting.CreateDefinitionSet(this.Application);

            InitializeComponent();

            ((AppSuiteApplication)App.Current).EnsureClosingToolTipIfWindowIsInactive(this.Get<Control>("testButton1"));

            this.logAction = new ScheduledAction(() =>
            {
                this.Logger.LogDebug("Time: {dateTime}", DateTime.Now);
                this.logAction?.Schedule(500);
            });

            this.integerTextBox = this.FindControl<IntegerTextBox>(nameof(integerTextBox)).AsNonNull();
            this.integerTextBox2 = this.FindControl<IntegerTextBox>(nameof(integerTextBox2)).AsNonNull();
            this.ipAddressTextBox = this.FindControl<IPAddressTextBox>(nameof(ipAddressTextBox)).AsNonNull();
            this.notificationPresenter = this.Get<NotificationPresenter>(nameof(notificationPresenter));
            this.Get<RegexTextBox>("regexTextBox").Also(it =>
            {
                // make a reference from RegexTextBox to MainWindow
                it.GetObservable(RegexTextBox.IsTextValidProperty).Subscribe(_ =>
                { });
            });

            var syntaxHighlightingDefSet = new SyntaxHighlightingDefinitionSet("C#").Also(defSet =>
            {
#pragma warning disable SYSLIB1045
                defSet.SpanDefinitions.Add(new SyntaxHighlightingSpan().Also(it => 
                { 
                    it.StartPattern = new("\"");
                    it.EndPattern = new("(?<=[^\\\\])\"");
                    it.Foreground = Brushes.Brown;
                    it.FontStyle = FontStyle.Italic;

                    it.TokenDefinitions.Add(new() { Pattern = new("\\\\\\S"), Foreground = Brushes.Orange });
                }));
                defSet.SpanDefinitions.Add(new SyntaxHighlightingSpan().Also(it => 
                { 
                    it.StartPattern = new("\\$\"");
                    it.EndPattern = new("(?<=[^\\\\])\"");
                    it.Foreground = Brushes.Brown;
                    it.FontStyle = FontStyle.Italic;

                    it.TokenDefinitions.Add(new() { Pattern = new("\\\\\\S"), Foreground = Brushes.Orange });
                    it.TokenDefinitions.Add(new() { Pattern = new("(\\{\\{|\\}\\})"), Foreground = Brushes.Yellow });
                    it.TokenDefinitions.Add(new() { Pattern = new("\\{([^\\{][^\\}]*)?\\}"), Foreground = Brushes.Yellow });
                }));
                defSet.SpanDefinitions.Add(new SyntaxHighlightingSpan().Also(it => 
                { 
                    it.StartPattern = new("'");
                    it.EndPattern = new("(?<=[^\\\\])'");
                    it.Foreground = Brushes.Brown;

                    it.TokenDefinitions.Add(new() { Pattern = new("\\\\\\S"), Foreground = Brushes.Orange });
                }));
                defSet.SpanDefinitions.Add(new SyntaxHighlightingSpan().Also(it => 
                { 
                    it.StartPattern = new("///");
                    it.EndPattern = new("(\\n|$)");
                    it.Foreground = Brushes.Gray;
                }));
                defSet.TokenDefinitions.Add(new() { Pattern = new("\\b(async|await|base|break|case|catch|class|continue|default|delegate|do|else|enum|event|finally|for|foreach|get|goto|if|internal|lock|nameof|namespace|new|sealed|set|static|struct|switch|this|throw|try|override|protected|public|private|readonly|return|using|var|virtual|volatile|while|yield)\\b"), Foreground = Brushes.Cyan });
                defSet.TokenDefinitions.Add(new() { Pattern = new("\\b(bool|byte|char|decimal|double|float|int|long|nint|nuint|short|string|uint|ulong|ushort)\\b"), Foreground = Brushes.Green });
                defSet.TokenDefinitions.Add(new() { Pattern = new("\\b[+-]?(0x[0-9a-fA-F]+|[0-9]+|0[0-7]+|[0-9]+\\.[0-9]+)[uU]?[lL]?\\b"), Foreground = Brushes.Orange });
                defSet.TokenDefinitions.Add(new() { Pattern = new("\\b(true|false)\\b"), Foreground = Brushes.Orange });
                defSet.TokenDefinitions.Add(new() { Pattern = new("//[^$\\n]*"), Foreground = Brushes.Green });
                //defSet.TokenDefinitions.Add(new() { Pattern = new("\"\""), FontStyle = FontStyle.Italic, Foreground = Brushes.Brown });
#pragma warning restore SYSLIB1045
            });

            this.Get<SyntaxHighlightingTextBlock>("multiLineSyntaxHighlightingTextBlock").Also(it =>
            {
                it.DefinitionSet = syntaxHighlightingDefSet;
                it.Text = """
                    Line 1
                    Line 2
                    Line 3 (should not be shown)
                    Line 4 (should not be shown)
                    """;
            });
            this.syntaxHighlightingTextBlock = this.Get<SyntaxHighlightingTextBlock>(nameof(syntaxHighlightingTextBlock)).Also(it =>
            {
                it.DefinitionSet = StringInterpolationFormatSyntaxHighlighting.CreateDefinitionSet(this.Application);
                //it.Text = "var s1 = \"Hello \\\"World\";\nvar s2 = \"\";\nvar s3 = \"\";\nvar c1 = '\\n';";
                //it.Text = "// Create regular expression for parsing base name.";
            });
            this.syntaxHighlightingRefTextBlock = this.Get<Avalonia.Controls.TextBlock>(nameof(syntaxHighlightingRefTextBlock)).Also(it =>
            {
                //
            });

            this.syntaxHighlightingTextBox = this.Get<SyntaxHighlightingTextBox>(nameof(syntaxHighlightingTextBox)).Also(it =>
            {
                it.DefinitionSet = new SyntaxHighlightingDefinitionSet("").Also(it =>
                {
                    it.TokenDefinitions.Add(new()
                    {
                        Foreground = Brushes.Red,
                    });
                });
            });

            var tabControl = this.FindControl<TabControl>("tabControl").AsNonNull();
            this.tabItems.AddRange(tabControl.Items!.Cast<TabItem>());
            tabControl.Items.Clear();
            tabControl.ItemsSource = this.tabItems;
            (this.tabItems[0].Header as Control)?.Let(it => this.Application.EnsureClosingToolTipIfWindowIsInactive(it));
        }


        public ViewModels.ApplicationOptions ApplicationOptions { get; } = new ViewModels.ApplicationOptions();


        public void EditConfiguration()
        {
            _ = new SettingsEditorDialog()
            {
                SettingKeys = SettingKey.GetDefinedKeys<ConfigurationKeys>(),
                Settings = this.Configuration,
            }.ShowDialog(this);
        }


        public async Task FindCommandPath()
        {
            var command = await new TextInputDialog
            {
                Message = "Command:"
            }.ShowDialog(this);
            if (string.IsNullOrWhiteSpace(command))
                return;
            try
            {
                var path = await CommandSearchPaths.FindCommandPathAsync(command);
                if (string.IsNullOrEmpty(path))
                {
                    _ = new MessageDialog
                    {
                        Icon = MessageDialogIcon.Warning,
                        Message = $"Command '{command}' not found."
                    }.ShowDialog(this);
                }
                else
                {
                    _ = new MessageDialog
                    {
                        Icon = MessageDialogIcon.Success,
                        Message = $"Command '{command}' found: {path}."
                    }.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                _ = new MessageDialog
                {
                    Icon = MessageDialogIcon.Error,
                    Message = $"{ex.GetType().Name}: {ex.Message}"
                }.ShowDialog(this);
            }
        }


        public IList<string> ImageIdList { get; }


        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


        //public override bool IsExtendingClientAreaAllowed => false;


        protected override void OnApplicationPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnApplicationPropertyChanged(e);
        }


        void OnAppLoadingStrings(IAppSuiteApplication? app, CultureInfo cultureInfo) =>
            this.SetupCustomResource(cultureInfo);


        protected override void OnClosed(EventArgs e)
        {
            this.hfProcessInfoUpdateToken.Dispose();
            this.logAction.Cancel();
            this.Application.LoadingStrings -= this.OnAppLoadingStrings;
            base.OnClosed(e);
        }


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
            var srcTabItem = this.tabItems[srcIndex];

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
            
            // [Workaround] Sometimes the content of tab item will gone after moving tab item
            (this.Content as Control)?.Let(it =>
            {
                it.Margin = new(0, 0, 0, -1);
                this.SynchronizationContext.PostDelayed(() =>
                {
                    it.Margin = new();
                }, 0);
            });
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


        public async void OpenFileForSyntaxHighlightingTextBlocks()
        {
            this.syntaxHighlightingTextBox.DefinitionSet!.TokenDefinitions[0].Let(it =>
            {
                if (it.Pattern == null)
                    it.Pattern = new("\\s*");
                else
                    it.Pattern = null;
            });
            return;

            using var stream = await (await this.StorageProvider.OpenFilePickerAsync(new()
            {
                //
            })).LetAsync(async it =>
            {
                if (it != null && it.Count == 1)
                    return await it[0].OpenReadAsync();
                return null;
            });
            if (stream == null)
                return;
            
            using var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8);
            this.syntaxHighlightingTextBlock.Text = await reader.ReadToEndAsync();
        }


        public SyntaxHighlightingDefinitionSet RegexSyntaxHighlightingDefinitionSet { get; }


        public void RestartApp()
        {
            this.Application.Restart();
        }


        public IImage? SelectedImage { get => this.selectedImage; }


        public string? SelectedImageId { get => this.selectedImageId; }


        void SetupCustomResource(CultureInfo cultureInfo)
        {
            this.overlayResourcesToken?.Dispose();
            this.overlayResourcesToken = this.Application.AddCustomResource(new ResourceDictionary().Also(it=>
            {
                it["String/TestMainWIndow.Title"] = $"Title ({cultureInfo.Name})";
            }));
        }

        public async void ShowAppInfoDialog()
        {
            //this.canShowAppInfo.Update(false);
            _ = this.Application.ShowApplicationInfoDialogAsync(this);
            //this.canShowAppInfo.Update(true);
        }

        public void ShowAppUpdateDialog() =>
            this.Application.CheckForApplicationUpdateAsync(this, true);
        
        public void ShowExternalDependenciesDialog() =>
            _ = new ExternalDependenciesDialog().ShowDialog(this);

        public async void ShowMessageDialog()
        {
            var icon = Enum.GetValues<MessageDialogIcon>().SelectRandomElement();
            var result = await new MessageDialog()
            {
                Buttons = Enum.GetValues<MessageDialogButtons>().SelectRandomElement(),
                CustomDoNotAskOrShowAgainText = "DO NOT SHOW AGAIN!!!",
                CustomIcon = icon == MessageDialogIcon.Custom ? this.FindResourceOrDefault<IImage>("Image/Icon.Star") : null,
                Description = "Description of dialog.",
                DoNotAskOrShowAgain = true,
                DoNotAskOrShowAgainDescription = "This is the description.",
                Icon = icon,
                Message = this.GetResourceObservable("String/MessageDialog.Message"),
                SecondaryMessage = "Secondary message...",
            }.ShowDialog(this);
            _ = new MessageDialog()
            {
                Message = new FormattedString().Also(it =>
                {
                    it.Arg1 = result;
                    it.Bind(FormattedString.FormatProperty, this.GetResourceObservable("String/MessageDialog.Result"));
                })
            }.ShowDialog(this);
        }

        public void ShowNotification()
        {
            this.notificationPresenter.AddNotification(new Notification().Also(it =>
            {
                it.Actions = new[]
                {
                    new NotificationAction
                    {
                        Command = new Command(this.ShowNotification),
                        Name = "Action1"
                    },
                    new NotificationAction
                    {
                        Command = new Command(this.ShowNotification),
                        Name = "Action2"
                    },
                    new NotificationAction
                    {
                        Command = new Command(this.ShowNotification),
                        Name = "Action3"
                    },
                    new NotificationAction
                    {
                        Command = new Command(this.ShowNotification),
                        Name = "Action4"
                    },
                    new NotificationAction
                    {
                        Command = new Command(this.ShowNotification),
                        Name = "Action5"
                    },
                };
                it.Bind(Notification.IconProperty, new CachedResource<object?>(this, "Image/Icon.Information.Colored"));
                it.Bind(Notification.MessageProperty, new FormattedString { Format = "Hello notification." });
                it.Title = "Title";
            }));
        }

        public ICommand ShowAppInfoDialogCommand { get; }

        public async void ShowTestDialog()
        {
            var result = await new Dialog().ShowDialog<ApplicationOptionsDialogResult>();
            if (result == ApplicationOptionsDialogResult.RestartMainWindowsNeeded)
                _ = this.Application.RestartRootWindowsAsync();
            else if (result == ApplicationOptionsDialogResult.RestartApplicationNeeded)
                this.Application.Restart();
        }

        public async void ShowTextInputDialog()
        {
            var result = await new TextInputDialog()
            {
                CheckBoxDescription = "Description",
                CheckBoxMessage = this.Application.GetObservableString("Common.DoNotShowAgain"),
                IsCheckBoxChecked = true,
                Message = "Message",
            }.ShowDialog(this);
        }


        void ShowTutorial(Visual anchor)
        {
            var tutorial = new Tutorial().Also(it =>
            {
                it.Anchor = anchor;
                it.Description = "This is a tutorial with long long long long description.";
                it.Dismissed += (_, e) => 
                { 
                    var nextTutorial = new Tutorial().Also(it =>
                    {
                        it.Anchor = this.FindControl<Control>("tutorialAnchorTextBlock");
                        it.Description = "This is the 2nd tutorial.";
                        it.Bind(Tutorial.IconProperty, this.GetResourceObservable("Image/Icon.Information"));
                        it.IsSkippingAllTutorialsAllowed = false;
                    });
                    this.ShowTutorial(nextTutorial);
                };
                it.Bind(Tutorial.IconProperty, this.GetResourceObservable("Image/Icon.Lightbulb.Colored"));
                it.SkippingAllTutorialRequested += (_, e) => 
                { };
            });
            this.ShowTutorial(tutorial);
        }


        public ICommand ShowTutorialCommand { get; }


        public void Shutdown() =>
            this.Application.Shutdown();
        

        public IEnumerable<object?> StringItems { get; } = new string[]
        {
            "Item 1",
            "Item 2",
            "Item 3",
            "Item 4",
        };


        public void SwitchAppCulture()
        {
            this.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, this.Settings.GetValueOrDefault(SettingKeys.Culture) switch
            {
                ApplicationCulture.System => ApplicationCulture.EN_US,
                ApplicationCulture.EN_US => ApplicationCulture.ZH_TW,
                ApplicationCulture.ZH_TW => ApplicationCulture.ZH_CN,
                _ => ApplicationCulture.System,
            });
        }


        public void SwitchTheme()
        {
            this.Settings.SetValue<ThemeMode>(SettingKeys.ThemeMode, this.Settings.GetValueOrDefault(SettingKeys.ThemeMode) switch
            {
                ThemeMode.System => ThemeMode.Dark,
                ThemeMode.Dark => ThemeMode.Light,
                _ => this.Application.IsSystemThemeModeSupported ? ThemeMode.System : ThemeMode.Dark,
            });
        }


        public void SwitchUsingCompactUI() =>
            this.Settings.SetValue<bool>(SettingKeys.UseCompactUserInterface, !this.Settings.GetValueOrDefault(SettingKeys.UseCompactUserInterface));
        

#if WINDOWS
        protected override Type? TaskbarManagerType => typeof(Microsoft.WindowsAPICodePack.Taskbar.TaskbarManager);
#endif


        public void Test()
        {
            _ = this.Application.TakeMemorySnapshotAsync(this);
            //_ = new EnableRunningScriptDialog().ShowDialog(this);
        }


        public void Test2()
        {
            this.Settings.SetValue<ApplicationCulture>(SettingKeys.Culture, this.Settings.GetValueOrDefault(SettingKeys.Culture) switch
            {
                ApplicationCulture.System => ApplicationCulture.EN_US,
                ApplicationCulture.EN_US => ApplicationCulture.ZH_TW,
                ApplicationCulture.ZH_TW => ApplicationCulture.ZH_CN,
                _ => ApplicationCulture.System,
            });
            /*
            this.Application.ShowMainWindow(window =>
            {
                System.Diagnostics.Debug.WriteLine("Window created");
            });
            */
        }


        public void UpdateTaskbarIconProgressState()
        {
            switch (this.TaskbarIconProgressState)
            {
                case TaskbarIconProgressState.None:
                    this.TaskbarIconProgress = 0.5;
                    this.TaskbarIconProgressState = TaskbarIconProgressState.Normal;
                    break;
                case TaskbarIconProgressState.Normal:
                    this.TaskbarIconProgress = 0.6;
                    this.TaskbarIconProgressState = TaskbarIconProgressState.Paused;
                    break;
                case TaskbarIconProgressState.Paused:
                    this.TaskbarIconProgress = 0.8;
                    this.TaskbarIconProgressState = TaskbarIconProgressState.Error;
                    break;
                /*
                case TaskbarIconProgressState.Error:
                    this.TaskbarIconProgress = 0.1;
                    this.TaskbarIconProgressState = TaskbarIconProgressState.Indeterminate;
                    break;
                */
                default:
                    this.TaskbarIconProgressState = TaskbarIconProgressState.None;
                    break;
            }
        }
    }
}
