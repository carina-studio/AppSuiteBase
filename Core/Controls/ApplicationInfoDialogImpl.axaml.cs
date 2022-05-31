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
		static readonly AvaloniaProperty<bool> HasApplicationChangeListProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasApplicationChangeList));
		static readonly AvaloniaProperty<bool> HasTotalPhysicalMemoryProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasTotalPhysicalMemory));
		static readonly SettingKey<bool> IsRestartingInDebugModeConfirmationShownKey = new("ApplicationInfoDialog.IsRestartingInDebugModeConfirmationShown");
		static readonly AvaloniaProperty<string?> VersionStringProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, string?>(nameof(VersionString));


		// Fields.
		readonly Panel badgesPanel;
		readonly Panel productListPanel;
		readonly EnumConverter productStateConverter;


		// Constructor.
		public ApplicationInfoDialogImpl()
		{
			AvaloniaXamlLoader.Load(this);
			this.badgesPanel = this.Get<Panel>(nameof(badgesPanel)).AsNonNull();
			this.productListPanel = this.Get<Panel>(nameof(productListPanel)).AsNonNull();
			this.productStateConverter = new(this.Application, typeof(ProductState));
			this.SetValue(HasTotalPhysicalMemoryProperty, this.Application.HardwareInfo.TotalPhysicalMemory.HasValue);
		}


		// Export application logs to file.
		async void ExportLogs()
		{
			// check state
			if (this.DataContext is not ApplicationInfo appInfo)
				return;

			// select file
			var fileName = await new SaveFileDialog().Also(it =>
			{
				var dateTime = DateTime.Now;
				it.Filters.Add(new FileDialogFilter().Also(filter =>
				{
					filter.Extensions.Add("zip");
					filter.Name = this.Application.GetString("FileFormat.Zip");
				}));
				it.InitialFileName = $"Logs-{dateTime.ToString("yyyyMMdd-HHmmss")}.zip";
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


		/// <inheritdoc/>
		protected override void OnClosed(EventArgs e)
		{
			this.Application.StringsUpdated -= this.OnAppStringsUpdated;
			this.Application.ProductManager.ProductStateChanged -= this.OnProductStateChanged;
			base.OnClosed(e);
		}


		// Called when application string resources updated.
		void OnAppStringsUpdated(object? sender, EventArgs e)
		{
			foreach (var child in this.productListPanel.Children)
			{
				if (child is Panel itemView)
					this.ShowProductInfo(itemView);
			}
		}


		/// <inheritdoc/>
		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);
			this.Application.StringsUpdated += this.OnAppStringsUpdated;
			this.Application.ProductManager.ProductStateChanged += this.OnProductStateChanged;
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


		// Property changed.
        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);
			if (change.Property == DataContextProperty)
			{
				if (change.NewValue.Value is ApplicationInfo appInfo)
				{
					// sync state
					this.Title = this.Application.GetFormattedString("ApplicationInfoDialog.Title", appInfo.Name);
					this.SetValue<string?>(VersionStringProperty, Global.Run(() =>
					{
						var buffer = new StringBuilder(this.Application.GetFormattedString("ApplicationInfoDialog.Version", appInfo.Version));
						if (appInfo.ReleasingType != ApplicationReleasingType.Stable)
						{
							buffer.Append(' ');
							buffer.Append(AppReleasingTypeConverter.Convert<string?>(appInfo.ReleasingType));
						}
						if (false)
						{
							buffer.Append(' ');
							buffer.Append(this.Application.GetString("ApplicationInfoDialog.ProprietaryVersion"));
						}
						return buffer.ToString();
					}));

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
									itemPanel.Children.Add(new TextBlock().Also(it =>
										it.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center));
									itemPanel.Children.Add(new Separator().Also(it =>
										it.Classes.Add("Dialog_Separator_Small")));
									itemPanel.Children.Add(new TextBlock().Also(it =>
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
							panel.Children.Add(new TextBlock() { Text = $"{assembly.GetName().Name} {assembly.GetName().Version}" });
						}
					});
				}
			}
        }


		// Restart in debug mode.
		async void RestartInDebugMode()
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
			this.Application.Restart($"{AppSuiteApplication.RestoreMainWindowsArgument} {AppSuiteApplication.DebugArgument}", this.Application.IsRunningAsAdministrator);
		}


		// Show change list of application.
		void ShowApplicationChangeList()
        {
			// check state
			if (this.DataContext is not ApplicationInfo appInfo)
				return;
			if (appInfo.ApplicationChangeList.ChangeList.IsEmpty())
				return;

			// show dialog
			_ = new ApplicationChangeListDialog(appInfo.ApplicationChangeList).ShowDialog(this);
		}


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
			if (!productManager.TryGetProductName(productId, out var name))
				name = productId;
			(view.Children[0] as TextBlock)?.Let(it => it.Text = name);

			// show state
			if (productManager.TryGetProductState(productId, out var state))
			{
				(view.Children[2] as TextBlock)?.Let(it => 
					it.Text = this.productStateConverter.Convert<string?>(state)?.Let(s =>
						$"({s})"));
			}
			else
				(view.Children[2] as TextBlock)?.Let(it => it.Text = null);
		}


        // String represent version.
        string? VersionString { get => this.GetValue<string?>(VersionStringProperty); }
	}
}