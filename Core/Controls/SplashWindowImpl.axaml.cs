using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CarinaStudio.Animation;
using CarinaStudio.Controls;
using CarinaStudio.Data.Converters;
using CarinaStudio.Threading;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
		static readonly StyledProperty<IBrush?> BackgroundImageOpacityMaskProperty = AvaloniaProperty.Register<SplashWindowImpl, IBrush?>(nameof(BackgroundImageOpacityMask));
		static readonly StyledProperty<IImage?> BackgroundImageProperty = AvaloniaProperty.Register<SplashWindowImpl, IImage?>(nameof(BackgroundImage));
		static readonly StyledProperty<IBitmap?> IconBitmapProperty = AvaloniaProperty.Register<SplashWindowImpl, IBitmap?>(nameof(IconBitmap));
		static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<SplashWindowImpl, string>(nameof(Message), " ", coerce: ((_, it) => string.IsNullOrEmpty(it) ? " " : it));


		// Fields.
		Color accentColor;
		double backgroundImageOpacity = 1.0;
		Uri? backgroundImageUri;
		Uri? iconUri;
		TaskCompletionSource? progressAnimationTaskSource;
		DoubleAnimator? progressAnimator;
		readonly ProgressBar progressBar;
		readonly ScheduledAction showAction;
		readonly Stopwatch stopwatch = new();


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
				var screen = this.Screens.ScreenFromWindow(this.PlatformImpl.AsNonNull());
				if (screen == null && this.stopwatch.ElapsedMilliseconds < MaxShowingRetryingDuration)
				{
					this.showAction?.Schedule(RetryShowingDelay);
					return;
				}

				// move to center of screen
				if (screen != null)
				{
					var screenBounds = screen.WorkingArea;
					var scaling = screen.Scaling;
					var width = this.Width;
					var height = this.Height;
					if (!Platform.IsMacOS)
					{
						width *= scaling;
						height *= scaling;
					}
					this.Position = new PixelPoint((int)((screenBounds.Width - width) / 2), (int)((screenBounds.Height - height) / 2));
				}
				
				// show content
				((Control)(this.Content.AsNonNull())).Opacity = 1;
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


		// Opacity of background image.
		public double BackgroundImageOpacity 
		{ 
			get => this.backgroundImageOpacity;
			set
			{
				if (value < 0)
					value = 0;
				else if (value > 1)
					value = 1;
				this.backgroundImageOpacity = value;
				this.SetValue<IBrush?>(BackgroundImageOpacityMaskProperty, new RadialGradientBrush().Also(it =>
				{
					it.Center = new(0.5, 1.0, RelativeUnit.Relative);
					it.GradientOrigin = it.Center;
					it.GradientStops.Add(new(Color.FromArgb((byte)(255 * value + 0.5), 255, 255, 255), 0));
					it.GradientStops.Add(new(Color.FromArgb(0, 255, 255, 255), 1));
					it.Radius = 0.6;
				}));
			}
		}


		// Opacity mask of background image.
		public IBrush? BackgroundImageOpacityMask { get => this.GetValue<IBrush?>(BackgroundImageOpacityMaskProperty); }


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
				value ??= new Uri($"avares://{AppSuiteApplication.Current.Assembly.GetName()}/AppIcon.ico");
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
					if (this.progressAnimationTaskSource != null)
					{
						this.progressAnimationTaskSource.SetResult();
						this.progressAnimationTaskSource = null;
					}
				}
				else
				{
					this.progressBar.IsIndeterminate = false;
					this.progressAnimationTaskSource ??= new();
					this.progressAnimator = new DoubleAnimator(this.progressBar.Value, Math.Max(0, Math.Min(1, value))).Also(it =>
					{
						it.Completed += (_, e) => 
						{
							this.progressBar.Value = it.EndValue;
							Dispatcher.UIThread.Post(async () =>
							{
								await Task.Delay(50);
								if (this.progressAnimator != it)
									return;
								this.progressAnimator = null;
								var taskSource = this.progressAnimationTaskSource;
								if (taskSource != null)
								{
									this.progressAnimationTaskSource = null;
									taskSource.SetResult();
								}
							}, DispatcherPriority.Normal);
						};
						it.Duration = TimeSpan.FromMilliseconds(250);
						it.Interpolator = Interpolators.Deceleration;
						it.ProgressChanged += (_, e) => this.progressBar.Value = it.Value;
						it.Start();
					});
				}
			}
		}


        /// <summary>
        /// String represents version.
        /// </summary>
        public string Version { get; }


		// Wait for completion of animation.
		public Task WaitForAnimationAsync()
		{
			if (this.progressAnimationTaskSource == null)
				return Task.CompletedTask;
			return this.progressAnimationTaskSource.Task;
		}
	}
}
