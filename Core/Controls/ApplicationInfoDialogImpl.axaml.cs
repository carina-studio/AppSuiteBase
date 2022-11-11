using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.Converters;
using CarinaStudio.AppSuite.Product;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Data.Converters;
using CarinaStudio.Threading;
using System;
using System.Linq;
using System.Text;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Application info dialog.
	/// </summary>
	partial class ApplicationInfoDialogImpl : Dialog
	{
		// Static fields.
		static readonly IValueConverter AppReleasingTypeConverter = new Converters.EnumConverter(AppSuiteApplication.Current, typeof(ApplicationReleasingType));
		static readonly StyledProperty<bool> HasApplicationChangeListProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasApplicationChangeList));
		static readonly StyledProperty<bool> HasExternalDependenciesProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>("HasExternalDependencies");
		static readonly StyledProperty<bool> HasTotalPhysicalMemoryProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasTotalPhysicalMemory));
		static readonly SettingKey<bool> IsRestartingInDebugModeConfirmationShownKey = new("ApplicationInfoDialog.IsRestartingInDebugModeConfirmationShown");
		static readonly DirectProperty<ApplicationInfoDialogImpl, PixelSize> PhysicalScreenSizeProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, PixelSize>("PhysicalScreenSize", w => w.physicalScreenSize);
		static readonly DirectProperty<ApplicationInfoDialogImpl, PixelRect> PhysicalScreenWorkingAreaProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, PixelRect>("PhysicalScreenWorkingArea", w => w.physicalScreenWorkingArea);
		public static readonly IValueConverter RectToStringConverter = new FuncValueConverter<object?, string?>(value =>
		{
			if (value is PixelRect pixelRect)
				return $"[{pixelRect.X}, {pixelRect.Y}, {pixelRect.Width}x{pixelRect.Height}]";
			if (value is Rect rect)
				return $"[{rect.X:F0}, {rect.Y:F0}, {rect.Width:F0}x{rect.Height:F0}]";
			return null;
		});
		static readonly DirectProperty<ApplicationInfoDialogImpl, double> ScreenPixelDensityProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, double>("ScreenPixelDensity", w => w.screenPixelDensity);
		static readonly DirectProperty<ApplicationInfoDialogImpl, Size> ScreenSizeProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, Size>("ScreenSize", w => w.screenSize);
		static readonly DirectProperty<ApplicationInfoDialogImpl, Rect> ScreenWorkingAreaProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, Rect>("ScreenWorkingArea", w => w.screenWorkingArea);
		public static readonly IValueConverter SizeToStringConverter = new FuncValueConverter<object?, string?>(value =>
		{
			if (value is PixelSize pixelSize)
				return $"{pixelSize.Width}x{pixelSize.Height}";
			if (value is Size size)
				return $"{size.Width:F0}x{size.Height:F0}";
			return null;
		});
		static readonly StyledProperty<string?> VersionStringProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, string?>(nameof(VersionString));


		// Fields.
		readonly Panel badgesPanel;
		readonly bool isProprietaryApp;
		PixelSize physicalScreenSize;
		PixelRect physicalScreenWorkingArea;
		IDisposable processInfoHfuToken = EmptyDisposable.Default;
		readonly Panel productListPanel;
		readonly EnumConverter productStateConverter;
		double screenPixelDensity = 1;
		Size screenSize;
		Rect screenWorkingArea;
		readonly ScheduledAction updateScreenInfoAction;


		// Constructor.
		public ApplicationInfoDialogImpl()
		{
			// check proprietary application
			foreach (var itf in this.Application.GetType().GetInterfaces())
			{
				if (itf.FullName == "CarinaStudio.AppSuite.IProprietaryApplication"
					&& itf.Assembly.FullName?.StartsWith("CarinaStudio.AppSuite.Proprietary,") == true)
				{
					this.isProprietaryApp = true;
					break;
				}
			}
			
			// setup controls
			AvaloniaXamlLoader.Load(this);
			this.badgesPanel = this.Get<Panel>(nameof(badgesPanel)).AsNonNull();
			this.productListPanel = this.Get<Panel>(nameof(productListPanel)).AsNonNull();
			this.productStateConverter = new(this.Application, typeof(ProductState));

			// setup actions
			this.updateScreenInfoAction = new(() =>
			{
				if (!this.IsOpened)
					return;
				var screen = this.Screens.ScreenFromWindow(this.PlatformImpl!) ?? this.Screens.ScreenFromVisual(this) ?? this.Screens.Primary;
				if (screen == null)
					return;
				var scaling = screen.Scaling;
				var screenSizePx = screen.Bounds.Size.Let(it =>
				{
					return Platform.IsMacOS
						? new PixelSize((int)(it.Width * scaling + 0.5), (int)(it.Height * scaling + 0.5))
						: it;
				});
				var screenSizeDip = screen.Bounds.Size.Let(it =>
				{
					return Platform.IsMacOS
						? new Size(it.Width, it.Height)
						: new Size(it.Width / scaling, it.Height / scaling);
				});
				var workingAreaPx = screen.WorkingArea.Let(it =>
				{
					return Platform.IsMacOS
						? new PixelRect((int)(it.X * scaling + 0.5), (int)(it.Y * scaling + 0.5), (int)(it.Width * scaling + 0.5), (int)(it.Height * scaling + 0.5))
						: it;
				});
				var workingAreaDip = screen.WorkingArea.Let(it =>
				{
					return Platform.IsMacOS
						? new Rect(it.X, it.Y, it.Width, it.Height)
						: new Rect(it.X / scaling, it.Y / scaling, it.Width / scaling, it.Height / scaling);
				});
				this.SetAndRaise<PixelSize>(PhysicalScreenSizeProperty, ref this.physicalScreenSize, screenSizePx);
				this.SetAndRaise<PixelRect>(PhysicalScreenWorkingAreaProperty, ref this.physicalScreenWorkingArea, workingAreaPx);
				this.SetAndRaise<double>(ScreenPixelDensityProperty, ref this.screenPixelDensity, scaling);
				this.SetAndRaise<Size>(ScreenSizeProperty, ref this.screenSize, screenSizeDip);
				this.SetAndRaise<Rect>(ScreenWorkingAreaProperty, ref this.screenWorkingArea, workingAreaDip);
			});

			// setup properties
			this.SetValue<bool>(HasExternalDependenciesProperty, this.Application.ExternalDependencies.ToArray().IsNotEmpty());
			this.SetValue<bool>(HasTotalPhysicalMemoryProperty, this.Application.HardwareInfo.TotalPhysicalMemory.HasValue);

			// observe properties
			this.GetObservable(BoundsProperty).Subscribe(_ => this.updateScreenInfoAction.Schedule(500));
		}


		/// <summary>
		/// Export application logs to file.
		/// </summary>
		public async void ExportLogs()
		{
			// check state
			if (this.DataContext is not ApplicationInfo appInfo)
				return;

			// select file
			var fileName = await new SaveFileDialog().Also(it =>
			{
				var dateTime = DateTime.Now;
				it.Filters!.Add(new FileDialogFilter().Also(filter =>
				{
					filter.Extensions.Add("zip");
					filter.Name = this.Application.GetString("FileFormat.Zip");
				}));
				it.InitialFileName = $"Logs-{dateTime:yyyyMMdd-HHmmss}.zip";
			}).ShowAsync(this);
			if (fileName == null)
				return;

			// export
			var success = await appInfo.ExportLogs(fileName);

			// show result
			if (!this.IsOpened)
				return;
			if (success)
			{
				_ = new MessageDialog()
				{
					Icon = MessageDialogIcon.Success,
					Message = this.Application.GetString("ApplicationInfoDialog.SucceededToExportAppLogs"),
				}.ShowDialog(this);
			}
			else
			{
				_ = new MessageDialog()
				{
					Icon = MessageDialogIcon.Error,
					Message = this.Application.GetString("ApplicationInfoDialog.FailedToExportAppLogs"),
				}.ShowDialog(this);
			}
		}


		// Check whether application change list is available or not.
		public bool HasApplicationChangeList { get => this.GetValue<bool>(HasApplicationChangeListProperty); }


		// Check whether total physical memory info is valid or not.
		public bool HasTotalPhysicalMemory { get => this.GetValue<bool>(HasTotalPhysicalMemoryProperty); }


		// Called when application string resources updated.
		void OnAppStringsUpdated(object? sender, EventArgs e)
		{
			foreach (var child in this.productListPanel.Children)
			{
				if (child is Panel itemView)
					this.ShowProductInfo(itemView);
			}
			this.UpdateTitle();
			this.UpdateVersionString();
		}


		/// <inheritdoc/>
		protected override void OnClosed(EventArgs e)
		{
			this.processInfoHfuToken.Dispose();
			this.Application.StringsUpdated -= this.OnAppStringsUpdated;
			this.Application.ProductManager.ProductStateChanged -= this.OnProductStateChanged;
			base.OnClosed(e);
		}


		/// <inheritdoc/>
		protected override void OnDataContextChanged(EventArgs e)
		{
			base.OnDataContextChanged(e);
			if (this.DataContext is ApplicationInfo appInfo)
			{
				// sync state
				this.UpdateTitle();
				this.UpdateVersionString();

				// show badges
				this.badgesPanel.Children.Clear();
				this.TryFindResource<double>("Double/ApplicationInfoDialog.AppBadge.Size", out var badgeSize);
				this.TryFindResource<Thickness>("Thickness/ApplicationInfoDialog.AppBadge.Margin", out var badgeMargin);
				foreach (var badge in appInfo.Badges)
				{
					this.badgesPanel.Children.Add(new Image()
					{
						Height = badgeSize.GetValueOrDefault(),
						Margin = badgeMargin.GetValueOrDefault(),
						Source = badge,
						Stretch = Avalonia.Media.Stretch.Uniform,
						Width = badgeSize.GetValueOrDefault(),
					});
				}

				// check change list
				this.SynchronizationContext.Post(async () =>
				{
					if (this.DataContext is not ApplicationInfo appInfo)
						return;
					await appInfo.ApplicationChangeList.WaitForChangeListReadyAsync();
					if (this.DataContext != appInfo)
						return;
					this.SetValue(HasApplicationChangeListProperty, appInfo.ApplicationChangeList.ChangeList.IsNotEmpty());
				});

				// show products
				this.productListPanel.Let(panel =>
				{
					panel.Children.Clear();
					if (appInfo.Products.IsNotEmpty() && !this.Application.ProductManager.IsMock)
					{
						foreach (var productId in appInfo.Products)
						{
							if (panel.Children.Count > 0)
							{
								panel.Children.Add(new Separator().Also(it => 
									it.Classes.Add("Dialog_Separator_Small")));
							}
							panel.Children.Add(new StackPanel().Also(itemPanel => 
							{ 
								itemPanel.DataContext = productId;
								itemPanel.Orientation = Avalonia.Layout.Orientation.Horizontal;
								itemPanel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
									it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center));
								itemPanel.Children.Add(new Separator().Also(it =>
									it.Classes.Add("Dialog_Separator_Small")));
								itemPanel.Children.Add(new Avalonia.Controls.TextBlock().Also(it =>
									it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center));
								this.ShowProductInfo(itemPanel);
							}));
						}
						this.FindControl<Panel>("productListSectionPanel")!.IsVisible = true;
					}
					else
						this.FindControl<Panel>("productListSectionPanel")!.IsVisible = false;
				});

				// show assemblies
				this.FindControl<Panel>("assembliesPanel")?.Let(panel =>
				{
					panel.Children.Clear();
					foreach (var assembly in appInfo.Assemblies)
					{
						if (panel.Children.Count > 0)
							panel.Children.Add(new Separator().Also(it => it.Classes.Add("Dialog_Separator_Small")));
						panel.Children.Add(new Avalonia.Controls.TextBlock() { Text = $"{assembly.GetName().Name} {assembly.GetName().Version}" });
					}
				});
			}
		}


		/// <inheritdoc/>
		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
			if (this.isProprietaryApp)
				this.Get<Panel>("scriptInfoPanel").IsVisible = true;
			this.processInfoHfuToken = this.Application.ProcessInfo.RequestHighFrequencyUpdate();
			this.Application.StringsUpdated += this.OnAppStringsUpdated;
			this.Application.ProductManager.ProductStateChanged += this.OnProductStateChanged;
			this.updateScreenInfoAction.Execute();
		}


		// Called when product state changed.
		void OnProductStateChanged(IProductManager? productManager, string productId)
		{
			foreach (var child in this.productListPanel.Children)
			{
				if (child is Panel itemView && itemView.DataContext as string == productId)
				{
					this.ShowProductInfo(itemView);
					break;
				}
			}
		}


		/// <summary>
		/// Restart in debug mode.
		/// </summary>
		public async void RestartInDebugMode()
		{
			// check state
			if (this.Application.IsDebugMode)
				return;
			
			// show message
			if (!this.PersistentState.GetValueOrDefault(IsRestartingInDebugModeConfirmationShownKey))
			{
				await new MessageDialog()
				{
					Icon = MessageDialogIcon.Information,
					Message = this.Application.GetString("ApplicationInfoDialog.ConfirmRestartingInDebugMode"),
				}.ShowDialog(this);
				this.PersistentState.SetValue<bool>(IsRestartingInDebugModeConfirmationShownKey, true);
			}

			// restart
			this.Close();
			this.SynchronizationContext.PostDelayed(() => // [Workaround] Prevent crashing on macOS if shutting down immediately after closing dialog.
				this.Application.Restart($"{AppSuiteApplication.RestoreMainWindowsArgument} {AppSuiteApplication.DebugArgument}", this.Application.IsRunningAsAdministrator),
				300);
		}


		/// <summary>
		/// Show change list of application.
		/// </summary>
		public void ShowApplicationChangeList()
        {
			// check state
			if (this.DataContext is not ApplicationInfo appInfo)
				return;
			if (appInfo.ApplicationChangeList.ChangeList.IsEmpty())
				return;

			// show dialog
			_ = new ApplicationChangeListDialog(appInfo.ApplicationChangeList).ShowDialog(this);
		}


		/// <summary>
		/// Show external dependencies.
		/// </summary>
		public void ShowExternalDependencies() =>
			new ExternalDependenciesDialog().ShowDialog(this);


		// Show product information on given view.
		void ShowProductInfo(Panel view)
		{
			// check state
			var productManager = this.Application.ProductManager;
			if (productManager.IsMock)
				return;
			if (this.DataContext is not ApplicationInfo appInfo)
				return;
			if (view.DataContext is not string productId)
				return;
			
			// show name
			if (!productManager.TryGetProductName(productId, out string name))
				name = productId;
			(view.Children[0] as Avalonia.Controls.TextBlock)?.Let(it => it.Text = name);

			// show state
			if (productManager.TryGetProductState(productId, out var state))
			{
				(view.Children[2] as Avalonia.Controls.TextBlock)?.Let(it => 
					it.Text = this.productStateConverter.Convert<string?>(state)?.Let(s =>
						$"({s})"));
			}
			else
				(view.Children[2] as Avalonia.Controls.TextBlock)?.Let(it => it.Text = null);
		}


		// Update title of window.
		void UpdateTitle()
		{
			if (this.DataContext is not ApplicationInfo appInfo)
				return;
			this.Title = this.Application.GetFormattedString("ApplicationInfoDialog.Title", appInfo.Name);
		}


		// Update version string.
		void UpdateVersionString()
		{
			if (this.DataContext is not ApplicationInfo appInfo)
				return;
			this.SetValue<string?>(VersionStringProperty, Global.Run(() =>
			{
				var buffer = new StringBuilder(this.Application.GetFormattedString("ApplicationInfoDialog.Version", appInfo.Version));
				if (appInfo.ReleasingType != ApplicationReleasingType.Stable)
				{
					buffer.Append(' ');
					buffer.Append(AppReleasingTypeConverter.Convert<string?>(appInfo.ReleasingType));
				}
				if (this.isProprietaryApp)
				{
					buffer.Append(' ');
					buffer.Append(this.Application.GetString("ApplicationInfoDialog.ProprietaryVersion"));
				}
				return buffer.ToString();
			}));
		}


        // String represent version.
        string? VersionString { get => this.GetValue<string?>(VersionStringProperty); }
	}
}