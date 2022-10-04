using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CarinaStudio.Animation;
using CarinaStudio.Controls;
using CarinaStudio.Data.Converters;
using CarinaStudio.Threading;
using System;
using System.Diagnostics;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Splash window when launching application.
	/// </summary>
	partial class SplashWindowImpl : Avalonia.Controls.Window
	{
		// Constants.
		const int MaxShowingRetryingDuration = 1000;
		const int RetryShowingDelay = 100;


		// Static fields.
		static readonly IValueConverter AppReleasingTypeConverter = new Converters.EnumConverter(AppSuiteApplication.Current, typeof(ApplicationReleasingType));
		static readonly AvaloniaProperty<IImage?> BackgroundImageProperty = AvaloniaProperty.Register<SplashWindowImpl, IImage?>(nameof(BackgroundImage));
		static readonly AvaloniaProperty<IBitmap?> IconBitmapProperty = AvaloniaProperty.Register<SplashWindowImpl, IBitmap?>(nameof(IconBitmap));
		static readonly AvaloniaProperty<string> MessageProperty = AvaloniaProperty.Register<SplashWindowImpl, string>(nameof(Message), " ", coerce: ((_, it) => string.IsNullOrEmpty(it) ? " " : it));


		// Fields.
		Color accentColor;
		Uri? backgroundImageUri;
		Uri? iconUri;
		DoubleAnimator? progressAnimator;
		readonly ProgressBar progressBar;
		readonly ScheduledAction showAction;
		readonly Stopwatch stopwatch = new Stopwatch();


		/// <summary>
		/// Initialize new <see cref="SplashWindowImpl"/>.
		/// </summary>
		public SplashWindowImpl()
		{
			var app = AppSuiteApplication.Current;
			this.ApplicationName = app.Name ?? "";
			this.Message = app.GetStringNonNull("SplashWindow.Launching");
			this.showAction = new(() =>
			{
				// get screen info
				var screen = this.Screens.ScreenFromWindow(this.PlatformImpl);
				if (screen == null && this.stopwatch.ElapsedMilliseconds < MaxShowingRetryingDuration)
				{
					this.showAction?.Schedule(RetryShowingDelay);
					return;
				}

				// move to center of screen
				if (screen != null)
				{
					var screenBounds = screen.WorkingArea;
					var pixelDensity = screen.PixelDensity;
					var width = this.Width;
					var height = this.Height;
					if (!Platform.IsMacOS)
					{
						width *= pixelDensity;
						height *= pixelDensity;
					}
					this.Position = new PixelPoint((int)((screenBounds.Width - width) / 2), (int)((screenBounds.Height - height) / 2));
				}
				
				// show content
				((Control)(this.Content)).Opacity = 1;
				this.Get<Avalonia.Controls.Control>("backgroundOverlayBorder").Let(control =>
				{
					control.Opacity = 1;
				});
				this.Get<Avalonia.Controls.Control>("iconImage").Let(control =>
				{
					control.Opacity = 1;
					(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
				});
				this.Get<Avalonia.Controls.Control>("titleTextBlock").Let(control =>
				{
					control.Opacity = 1;
					(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
				});
				this.Get<Avalonia.Controls.Control>("versionTextBlock").Let(control =>
				{
					this.TryFindResource<double>("Double/ApplicationInfoDialog.AppVersion.Opacity", out var opacity);
					control.Opacity = opacity ?? 1;
					(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
				});
				this.Get<Avalonia.Controls.Control>("copyrightTextBlock").Let(control =>
				{
					this.TryFindResource<double>("Double/ApplicationInfoDialog.AppVersion.Opacity", out var opacity);
					control.Opacity = opacity ?? 1;
					(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
				});
				this.Get<Avalonia.Controls.Control>("messagePanel").Let(control =>
				{
					control.Opacity = 1;
					(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
				});
			});
			this.Version = app.GetFormattedString("ApplicationInfoDialog.Version", app.Assembly.GetName().Version).AsNonNull();
			if (app.ReleasingType != ApplicationReleasingType.Stable)
				this.Version += $" {AppReleasingTypeConverter.Convert<string?>(app.ReleasingType)}";
			AvaloniaXamlLoader.Load(this);
			this.progressBar = this.Get<ProgressBar>(nameof(progressBar));
			if (Platform.IsWindows && !Platform.IsWindows8OrAbove)
			{
				this.Get<Panel>("rootPanel").Margin = default;
				this.Get<Border>("backgroundBorder").Let(it =>
				{
					it.BoxShadow = default;
					it.CornerRadius = default;
				});
				this.Get<Border>("backgroundOverlayBorder").CornerRadius = default;
				this.Get<Border>("border").CornerRadius = default;
			}
			this.Styles.Add((Avalonia.Styling.IStyle)(app.EffectiveThemeMode switch
			{
				ThemeMode.Light => this.Resources["lightTheme"].AsNonNull(),
				_ => this.Resources["darkTheme"].AsNonNull(),
			}));
		}


		// Accent color.
		public Color AccentColor
		{
			get => this.accentColor;
			set
			{
				this.accentColor = value;
				this.Resources["AccentColor30"] = Color.FromArgb((byte)(value.A * 0.3 + 0.5), value.R, value.G, value.B);
				this.Resources["AccentColor00"] = Color.FromArgb(0, value.R, value.G, value.B);
				this.progressBar.Foreground = new SolidColorBrush(value);
			}
		}


		// Name of application.
		public string ApplicationName { get; }


		// Background image.
		public IImage? BackgroundImage { get => this.GetValue<IImage?>(BackgroundImageProperty); }


		// Get or set URI of background image.
		public Uri? BackgroundImageUri
		{
			get => this.backgroundImageUri;
			set
			{
				this.VerifyAccess();
				if (this.backgroundImageUri == value)
					return;
				this.backgroundImageUri = value;
				if (value != null)
				{
					this.SetValue<IImage?>(BackgroundImageProperty, AvaloniaLocator.Current.GetService<IAssetLoader>()?.Let(loader =>
					{
						return loader.Open(value).Use(stream => new Bitmap(stream));
					}));
				}
				else
					this.SetValue<IImage?>(BackgroundImageProperty, null);
			}
		}


		// Get icon as IBitmap.
		public IBitmap? IconBitmap { get => this.GetValue<IBitmap?>(IconBitmapProperty); }


		// Get or set URI of application icon.
		public Uri? IconUri
        {
			get => this.iconUri;
			set
            {
				this.VerifyAccess();
				if (this.iconUri == value)
					return;
				this.iconUri = value;
				value = value ?? new Uri($"avares://{AppSuiteApplication.Current.Assembly.GetName()}/AppIcon.ico");
				this.Icon = AvaloniaLocator.Current.GetService<IAssetLoader>()?.Let(loader =>
				{
					return loader.Open(value).Use(stream => new WindowIcon(stream));
				});
				this.SetValue<IBitmap?>(IconBitmapProperty, AvaloniaLocator.Current.GetService<IAssetLoader>()?.Let(loader =>
				{
					return loader.Open(value).Use(stream => new Bitmap(stream));
				}));
			}
        }


		/// <summary>
		/// Get or set message to show.
		/// </summary>
		public string Message
		{
			get => this.GetValue<string>(MessageProperty);
			set => this.SetValue<string>(MessageProperty, value);
		}


		// Called when closed.
		protected override void OnClosed(EventArgs e)
		{
			this.progressAnimator?.Cancel();
			this.showAction.Cancel();
			this.stopwatch.Stop();
			base.OnClosed(e);
		}


		// Called when opened.
		protected override void OnOpened(EventArgs e)
		{
			// call base
			base.OnOpened(e);

			// show window
			this.stopwatch.Start();
			this.showAction.Schedule();
		}


		// Get or set progress.
		public double Progress
		{
			get 
			{
				if (this.progressAnimator != null)
					return progressAnimator.EndValue;
				return this.progressBar.IsIndeterminate ? double.NaN : this.progressBar.Value;
			}
			set
			{
				this.VerifyAccess();
				this.progressAnimator?.Cancel();
				if (double.IsNaN(value))
				{
					this.progressBar.IsIndeterminate = true;
					this.progressAnimator = null;
				}
				else
				{
					this.progressBar.IsIndeterminate = false;
					this.progressAnimator = new DoubleAnimator(this.progressBar.Value, Math.Max(0, Math.Min(1, value))).Also(it =>
					{
						it.Completed += (_, e) => this.progressBar.Value = it.EndValue;
						it.Duration = TimeSpan.FromMilliseconds(300);
						it.Interpolator = Interpolators.Deceleration;
						it.ProgressChanged += (_, e) => this.progressBar.Value = it.Value;
						it.Start();
					});
				}
			}
		}


        // String represents version.
        string Version { get; }
	}
}
