//#define ALLOW_MOCK_PRODUCT_MANAGER

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using CarinaStudio.AppSuite.Converters;
using CarinaStudio.AppSuite.Net;
using CarinaStudio.AppSuite.Product;
using CarinaStudio.AppSuite.Scripting;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using CarinaStudio.Data.Converters;
using CarinaStudio.IO;
using CarinaStudio.Threading;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Application info dialog.
	/// </summary>
	class ApplicationInfoDialogImpl : Dialog
	{
		/// <summary>
		/// Converter to convert from <see cref="System.Net.NetworkInformation.NetworkInterfaceType"/> to string.
		/// </summary>
		public static readonly IValueConverter NetworkInterfaceTypeConverter = new EnumConverter(AppSuiteApplication.CurrentOrNull, typeof(System.Net.NetworkInformation.NetworkInterfaceType));


		// Static fields.
		static readonly IValueConverter AppReleasingTypeConverter = new EnumConverter(AppSuiteApplication.Current, typeof(ApplicationReleasingType));
		static readonly StyledProperty<bool> HasApplicationChangeListProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasApplicationChangeList));
		static readonly StyledProperty<bool> HasExternalDependenciesProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>("HasExternalDependencies");
		static readonly StyledProperty<bool> HasPrivacyPolicyProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>("HasPrivacyPolicy");
		static readonly StyledProperty<bool> HasTotalPhysicalMemoryProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasTotalPhysicalMemory));
		static readonly StyledProperty<bool> HasUserAgreementProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>("HasUserAgreement");
		static readonly SettingKey<bool> IsRestartingInDebugModeConfirmationShownKey = new("ApplicationInfoDialog.IsRestartingInDebugModeConfirmationShown");
		static readonly DirectProperty<ApplicationInfoDialogImpl, PixelSize> PhysicalScreenSizeProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, PixelSize>(nameof(PhysicalScreenSize), w => w.physicalScreenSize);
		static readonly DirectProperty<ApplicationInfoDialogImpl, PixelRect> PhysicalScreenWorkingAreaProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, PixelRect>(nameof(PhysicalScreenWorkingArea), w => w.physicalScreenWorkingArea);
		static readonly StyledProperty<string?> PrimaryNetworkInterfacePhysicalAddressProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, string?>("PrimaryNetworkInterfacePhysicalAddress");
		public static readonly IValueConverter RectToStringConverter = new FuncValueConverter<object?, string?>(value =>
		{
			if (value is PixelRect pixelRect)
				return $"[{pixelRect.X}, {pixelRect.Y}, {pixelRect.Width}x{pixelRect.Height}]";
			if (value is Rect rect)
				return $"[{rect.X:F0}, {rect.Y:F0}, {rect.Width:F0}x{rect.Height:F0}]";
			return null;
		});
		static readonly DirectProperty<ApplicationInfoDialogImpl, double> ScreenPixelDensityProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, double>(nameof(ScreenPixelDensity), w => w.screenPixelDensity);
		static readonly DirectProperty<ApplicationInfoDialogImpl, Size> ScreenSizeProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, Size>(nameof(ScreenSize), w => w.screenSize);
		static readonly DirectProperty<ApplicationInfoDialogImpl, Rect> ScreenWorkingAreaProperty = AvaloniaProperty.RegisterDirect<ApplicationInfoDialogImpl, Rect>(nameof(ScreenWorkingArea), w => w.screenWorkingArea);
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
		readonly TaskCompletionSource closingTaskSource = new();
		readonly bool isProprietaryApp;
		PixelSize physicalScreenSize;
		PixelRect physicalScreenWorkingArea;
		IDisposable processInfoHfuToken = EmptyDisposable.Default;
		readonly Panel productListPanel;
		double screenPixelDensity = 1;
		Size screenSize;
		Rect screenWorkingArea;
		readonly ScheduledAction updateScreenInfoAction;


		// Constructor.
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(NetworkManager))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ProcessInfo))]
		[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(ScriptManager))]
		[DynamicDependency(nameof(Copyright))]
		[DynamicDependency(nameof(DeactivateProduct))]
		[DynamicDependency(nameof(ExportLogs))]
		[DynamicDependency(nameof(RestartInDebugMode))]
		[DynamicDependency(nameof(ShowApplicationChangeList))]
		[DynamicDependency(nameof(ShowAppUpdateDialog))]
		[DynamicDependency(nameof(ShowExternalDependencies))]
		[DynamicDependency(nameof(ShowPrivacyPolicy))]
		[DynamicDependency(nameof(ShowUserAgreement))]
		[DynamicDependency(nameof(TakeMemorySnapshot))]
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

			// setup copyright
			this.Copyright = ((this.Application as AppSuiteApplication)?.CopyrightBeginningYear ?? AppSuiteApplication.CopyrightEndingYear).Let(beginningYear =>
			{
				if (beginningYear < AppSuiteApplication.CopyrightEndingYear)
					return $"©{beginningYear}-{AppSuiteApplication.CopyrightEndingYear} Carina Studio";
				return $"©{AppSuiteApplication.CopyrightEndingYear} Carina Studio";
			});

			// check user agreement and privacy policy
			this.SetValue(HasPrivacyPolicyProperty, this.Application.PrivacyPolicyVersion != null && this.Application.PrivacyPolicy != null);
			this.SetValue(HasUserAgreementProperty, this.Application.UserAgreementVersion != null && this.Application.UserAgreement != null);

			// check MAC address
			Net.NetworkManager.Default.PrimaryPhysicalAddress?.Let(address =>
			{
				var buffer = new StringBuilder();
				foreach (var b in address.GetAddressBytes())
				{
					if (buffer.Length > 0)
						buffer.Append(':');
					buffer.AppendFormat("{0:x2}", b);
				}
				this.SetValue(PrimaryNetworkInterfacePhysicalAddressProperty, buffer.ToString());
			});
			
			// setup controls
			AvaloniaXamlLoader.Load(this);
			this.badgesPanel = this.Get<Panel>(nameof(badgesPanel)).AsNonNull();
			this.Get<Panel>("itemsPanel").Also(it =>
			{
				it.AddHandler(PointerPressedEvent, (_, e) =>
				{
					if (e.Source is not Avalonia.Controls.SelectableTextBlock)
						it.Focus();
				}, Avalonia.Interactivity.RoutingStrategies.Tunnel);
			});
			this.productListPanel = this.Get<Panel>(nameof(productListPanel)).AsNonNull();

			// setup actions
			this.updateScreenInfoAction = new(() =>
			{
				if (!this.IsOpened)
					return;
				var screen = this.Screens.ScreenFromWindow(this) ?? this.Screens.Primary;
				if (screen is null)
					return;
				var screenBounds = screen.Bounds;
				var scaling = screen.Scaling;
				if (Platform.IsMacOS)
				{
					var windowBounds = this.Bounds;
					if (windowBounds.Width > 0 || windowBounds.Height > 0)
					{
						var displayId = 0u;
						unsafe
						{
							var displayCount = 0u;
							Native.MacOS.CGGetDisplaysWithRect(new(windowBounds.X, windowBounds.Y, windowBounds.Width, windowBounds.Height), 1, &displayId, &displayCount);
							if (displayCount == 0)
								displayId = Native.MacOS.CGMainDisplayID();
						}
						var displayModeRef = Native.MacOS.CGDisplayCopyDisplayMode(displayId);
						if (displayModeRef != default)
						{
							try
							{
								var displayWidth = Native.MacOS.CGDisplayModeGetPixelWidth(displayModeRef);
								if (displayWidth > 0)
									scaling = (double)displayWidth / screenBounds.Width;
							}
							finally
							{
								Native.MacOS.CGDisplayModeRelease(displayModeRef);
							}
						}
					}
				}
				var screenSizePx = Platform.IsMacOS
					? new PixelSize((int)(screenBounds.Width * scaling + 0.5), (int)(screenBounds.Height * scaling + 0.5))
					: screenBounds.Size;
				var screenSizeDip = Platform.IsMacOS
					? new Size(screenBounds.Width, screenBounds.Height)
					: new Size(screenBounds.Width / scaling, screenBounds.Height / scaling);
				var workingAreaPx = screen.WorkingArea.Let(it => Platform.IsMacOS
					? new PixelRect((int)(it.X * scaling + 0.5), (int)(it.Y * scaling + 0.5), (int) (it.Width * scaling + 0.5), (int) (it.Height * scaling + 0.5))
					: it);
				var workingAreaDip = screen.WorkingArea.Let(it => Platform.IsMacOS
					? new Rect(it.X, it.Y, it.Width, it.Height)
					: new Rect(it.X / scaling, it.Y / scaling, it.Width / scaling, it.Height / scaling));
				this.SetAndRaise(PhysicalScreenSizeProperty, ref this.physicalScreenSize, screenSizePx);
				this.SetAndRaise(PhysicalScreenWorkingAreaProperty, ref this.physicalScreenWorkingArea, workingAreaPx);
				this.SetAndRaise(ScreenPixelDensityProperty, ref this.screenPixelDensity, scaling);
				this.SetAndRaise(ScreenSizeProperty, ref this.screenSize, screenSizeDip);
				this.SetAndRaise(ScreenWorkingAreaProperty, ref this.screenWorkingArea, workingAreaDip);
			});

			// setup properties
			this.SetValue(HasExternalDependenciesProperty, this.Application.ExternalDependencies.ToArray().IsNotEmpty());
			this.SetValue(HasTotalPhysicalMemoryProperty, this.Application.HardwareInfo.TotalPhysicalMemory.HasValue);

			// observe properties
			this.GetObservable(BoundsProperty).Subscribe(_ => this.updateScreenInfoAction.Schedule(500));
		}


		/// <summary>
		/// Copyright.
		/// </summary>
		public string Copyright { get; }
		
		
		// Deactivate product.
		void DeactivateProduct(string id) =>
			this.Application.ProductManager.DeactivateAndRemoveDeviceAsync(id, this);


		/// <summary>
		/// Export application logs to file.
		/// </summary>
		public async void ExportLogs()
		{
			// check state
			if (this.DataContext is not ApplicationInfo appInfo)
				return;

			// select file
			var options = new FilePickerSaveOptions().Also(options =>
			{
				var dateTime = DateTime.Now;
				options.FileTypeChoices = new[]
				{
					new FilePickerFileType(this.Application.GetStringNonNull("FileFormat.Zip")).Also(type =>
					{
						type.Patterns = new[] { "*.zip" };
					}),
				};
				options.SuggestedFileName = $"{this.Application.Name}-Logs-{dateTime:yyyyMMdd-HHmmss}.zip";
			});
			var fileName = (await this.StorageProvider.SaveFilePickerAsync(options))?.Let(it =>
			{
				var path = it.TryGetLocalPath();
				if (!PathEqualityComparer.Default.Equals(Path.GetExtension(path), ".zip"))
					path += ".zip";
				return path;
			});
			if (string.IsNullOrEmpty(fileName))
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
		public bool HasApplicationChangeList => this.GetValue(HasApplicationChangeListProperty);


		// Check whether total physical memory info is valid or not.
		public bool HasTotalPhysicalMemory => this.GetValue(HasTotalPhysicalMemoryProperty);


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
			this.closingTaskSource.SetResult();
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
						Stretch = Stretch.Uniform,
						Width = badgeSize.GetValueOrDefault(),
					});
				}

				// check change list
				this.SetValue(HasApplicationChangeListProperty, this.Application.ChangeList != null);

				// show products
				this.productListPanel.Let(panel =>
				{
					panel.Children.Clear();
#if ALLOW_MOCK_PRODUCT_MANAGER
					if (appInfo.Products.IsNotEmpty())
#else
					if (appInfo.Products.IsNotEmpty() && !this.Application.ProductManager.IsMock)
#endif
					{
						foreach (var productId in appInfo.Products)
						{
							if (panel.Children.Count > 0)
							{
								panel.Children.Add(new Separator().Also(it => 
									it.Classes.Add("Dialog_Item_Separator")));
							}
							panel.Children.Add(new StackPanel().Also(stackPanel =>
							{
								stackPanel.Tag = productId;
								stackPanel.Children.Add(new CompactDialogItemGrid().Also(itemPanel =>
								{
									itemPanel.Children.Add(new Avalonia.Controls.SelectableTextBlock().Also(it =>
									{
										it.Classes.Add("Dialog_TextBlock_Label");
									}));
									itemPanel.Children.Add(new Avalonia.Controls.SelectableTextBlock().Also(it =>
									{
										it.Classes.Add("Dialog_TextBlock");
										it.TextTrimming = TextTrimming.CharacterEllipsis;
										Grid.SetColumn(it, 1);
									}));
								}));
								var deactivateButton = new Button().Also(it =>
								{
									it.Classes.Add("Dialog_Item_Button");
									it.Click += (_, _) => this.DeactivateProduct(productId);
									it.Bind(ContentProperty, this.Application.GetObservableString("ApplicationInfoDialog.DeactivateProduct"));
								});
								stackPanel.Children.Add(new Line().Also(it =>
								{
									it.Classes.Add("Dialog_Item_Separator_Inner");
									it.Bind(IsVisibleProperty, deactivateButton.GetObservable(IsVisibleProperty));
								}));
								stackPanel.Children.Add(deactivateButton);
								this.ShowProductInfo(stackPanel);
							}));
						}
						this.Get<Panel>("productListSectionPanel").IsVisible = true;
					}
					else
						this.Get<Panel>("productListSectionPanel").IsVisible = false;
				});

				// show assemblies
				this.FindControl<Panel>("assembliesPanel")?.Let(panel =>
				{
					panel.Children.Clear();
					foreach (var assembly in appInfo.Assemblies)
					{
						if (panel.Children.Count > 0)
							panel.Children.Add(new Separator().Also(it => it.Classes.Add("Dialog_Item_Separator")));
						var assemblyName = assembly.GetName();
						var assemblyVersion = assemblyName.Version ?? new Version();
						panel.Children.Add(new CompactDialogItemGrid().Also(it =>
						{
							it.Children.Add(new Avalonia.Controls.SelectableTextBlock().Also(it =>
							{
								it.Classes.Add("Dialog_TextBlock_Label");
								it.Text = assemblyName.Name;
							}));
							if (assemblyVersion.Major != 0 
								|| assemblyVersion.Minor != 0 
								|| assemblyVersion.Revision != 0 
								|| assemblyVersion.Build != 0)
							{
								it.Children.Add(new Avalonia.Controls.SelectableTextBlock().Also(it =>
								{
									it.Text = assemblyVersion.ToString();
									Grid.SetColumn(it, 1);
								}));
							}
						}));
					}
				});
			}
		}


		/// <inheritdoc/>
		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
			this.processInfoHfuToken = this.Application.ProcessInfo.RequestHighFrequencyUpdate();
			this.Application.StringsUpdated += this.OnAppStringsUpdated;
			this.Application.ProductManager.ProductStateChanged += this.OnProductStateChanged;
		}


		/// <inheritdoc/>
		protected override void OnOpening(EventArgs e)
		{
			base.OnOpening(e);
			if (this.isProprietaryApp)
				this.Get<Panel>("scriptInfoPanel").IsVisible = true;
			this.updateScreenInfoAction.Execute();
		}


		// Called when product state changed.
		void OnProductStateChanged(IProductManager? productManager, string productId)
		{
			foreach (var child in this.productListPanel.Children)
			{
				if (child is Panel itemView && itemView.Tag as string == productId)
				{
					this.ShowProductInfo(itemView);
					break;
				}
			}
		}


		// Screen size in pixels.
		public PixelSize PhysicalScreenSize => this.physicalScreenSize;


		// Screen working area in pixels.
		public PixelRect PhysicalScreenWorkingArea => this.physicalScreenWorkingArea;


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
			{
				var argsBuilder = this.Application.CreateApplicationArgsBuilder().Also(it => it.IsDebugMode = true);
				this.Application.Restart(argsBuilder);
			}, 300);
		}


		// Pixel density of current screen.
		public double? ScreenPixelDensity => this.screenPixelDensity;


		// Size of current screen.
		public Size ScreenSize => this.screenSize;


		// Working area of screen.
		public Rect ScreenWorkingArea => this.screenWorkingArea;


		/// <summary>
		/// Show change list of application.
		/// </summary>
		public void ShowApplicationChangeList()
        {
			_ = new DocumentViewerWindow
			{
				DocumentSource = this.Application.ChangeList,
				Topmost = this.Topmost,
			}.ShowDialog(this);
		}


		/// <summary>
		/// Show application update dialog.
		/// </summary>
		public void ShowAppUpdateDialog()
		{
			if (this.Application is IAppSuiteApplication asApp)
				_ = asApp.CheckForApplicationUpdateAsync(this, true);
		}


		/// <summary>
		/// Show external dependencies.
		/// </summary>
		public void ShowExternalDependencies() =>
			new ExternalDependenciesDialog().ShowDialog(this);
		

		/// <summary>
		/// Show Privacy Policy.
		/// </summary>
		public void ShowPrivacyPolicy()
		{
			var documentSource = this.Application.PrivacyPolicy;
			if (documentSource == null)
				return;
			_ = new AgreementDialog()
			{
				DocumentSource = documentSource,
				IsAgreedBefore = true,
				Message = this.GetResourceObservable("String/ApplicationInfoDialog.PrivacyPolicyWasAgreedBefore"),
				Title = this.GetResourceObservable("String/Common.PrivacyPolicy"),
			}.ShowDialog(this);
		}


		// Show product information on given view.
		void ShowProductInfo(Panel view)
		{
			// check state
			var productManager = this.Application.ProductManager;
#if !ALLOW_MOCK_PRODUCT_MANAGER
			if (productManager.IsMock)
				return;
#endif
			if (this.DataContext is not ApplicationInfo)
				return;
			if (view.Tag is not string productId)
				return;
			
			// get state
			if (!productManager.TryGetProductState(productId, out var state))
				state = ProductState.Deactivated;
			
			// show name
			var itemGrid = (Grid)view.Children.First(it => it is Grid);
			if (!productManager.TryGetProductName(productId, out string? name))
				name = productId;
			itemGrid.Children[0].TryCastAndRun<Avalonia.Controls.TextBlock>(it => it.Text = name);
			
			// show authorization state
			var deactivateButton = view.Children.First(it => it is Button);
			if (state == ProductState.Activated 
				&& productManager.TryGetProductEmailAddress(productId, out var emailAddress))
			{
				itemGrid.Children[1].TryCastAndRun<Avalonia.Controls.TextBlock>(it => 
				{
					it.IsVisible = true;
					it.Text = this.Application.GetFormattedString("ApplicationInfoDialog.ProductAuthorizationInfo", emailAddress);
				});
				deactivateButton.IsVisible = true;
			}
			else
			{
				itemGrid.Children[1].TryCastAndRun<Control>(it => it.IsVisible = false);
				deactivateButton.IsVisible = false;
			}
		}


		/// <summary>
		/// Show User Agreement.
		/// </summary>
		public void ShowUserAgreement()
		{
			var documentSource = this.Application.UserAgreement;
			if (documentSource == null)
				return;
			_ = new AgreementDialog()
			{
				DocumentSource = documentSource,
				IsAgreedBefore = true,
				Message = this.GetResourceObservable("String/ApplicationInfoDialog.UserAgreementWasAgreedBefore"),
				Title = this.GetResourceObservable("String/Common.UserAgreement"),
			}.ShowDialog(this);
		}


		/// <summary>
		/// Take memory snapshot.
		/// </summary>
		public void TakeMemorySnapshot() =>
			(this.Application as AppSuiteApplication)?.TakeMemorySnapshotAsync(this);


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
			this.SetValue(VersionStringProperty, Global.Run(() =>
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
        string? VersionString => this.GetValue(VersionStringProperty);


        /// <summary>
		/// Wait for closing dialog.
		/// </summary>
		/// <returns>Task of waiting.</returns>
		public Task WaitForClosingAsync() =>
			this.closingTaskSource.Task;
	}
}