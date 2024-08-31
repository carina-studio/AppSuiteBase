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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls;

/// <summary>
/// Splash window when launching application.
/// </summary>
class SplashWindowImpl : Avalonia.Controls.Window
{
	// Constants.
	const int MaxShowingRetryingDuration = 1000;
	const int RetryShowingDelay = 100;


	// Static fields.
	static readonly DirectProperty<SplashWindowImpl, Color> AccentColorProperty = AvaloniaProperty.RegisterDirect<SplashWindowImpl, Color>(nameof(AccentColor), w => w.accentColor);
	static readonly IValueConverter AppReleasingTypeConverter = new Converters.EnumConverter(IAppSuiteApplication.Current, typeof(ApplicationReleasingType));
	static readonly DirectProperty<SplashWindowImpl, double> BackgroundImageOpacityProperty = AvaloniaProperty.RegisterDirect<SplashWindowImpl, double>(nameof(BackgroundImageOpacity), w => w.backgroundImageOpacity);
	static readonly StyledProperty<IImage?> BackgroundImageProperty = AvaloniaProperty.Register<SplashWindowImpl, IImage?>(nameof(BackgroundImage));
	static readonly StyledProperty<Bitmap?> IconBitmapProperty = AvaloniaProperty.Register<SplashWindowImpl, Bitmap?>(nameof(IconBitmap));
	static readonly StyledProperty<string> MessageProperty = AvaloniaProperty.Register<SplashWindowImpl, string>(nameof(Message), " ", coerce: (_, it) => string.IsNullOrEmpty(it) ? " " : it);
	static readonly StyledProperty<Color> MessageColorProperty = AvaloniaProperty.Register<SplashWindowImpl, Color>(nameof(MessageColor));


	// Fields.
	Color accentColor;
	double backgroundImageOpacity = 1.0;
	Uri? backgroundImageUri;
	Uri? iconUri;
	readonly TaskCompletionSource initAnimationTaskCompletionSource = new(); 
	TaskCompletionSource? progressAnimationTaskSource;
	DoubleRenderingAnimator? progressAnimator;
	readonly ProgressBar progressBar;
	TaskCompletionSource? renderingTaskSource;
	readonly ScheduledAction showAction;
	readonly Stopwatch stopwatch = new();


	/// <summary>
	/// Initialize new <see cref="SplashWindowImpl"/>.
	/// </summary>
	[DynamicDependency(nameof(ApplicationName))]
	[DynamicDependency(nameof(Copyright))]
	[DynamicDependency(nameof(Version))]
	public SplashWindowImpl()
	{
		var app = IAppSuiteApplication.CurrentOrNull as AppSuiteApplication;
		this.ApplicationName = app?.Name ?? "";
		this.Copyright = (app?.CopyrightBeginningYear)?.Let(beginningYear =>
		{
			if (beginningYear < AppSuiteApplication.CopyrightEndingYear)
				return $"©{beginningYear}-{AppSuiteApplication.CopyrightEndingYear} Carina Studio";
			return $"©{AppSuiteApplication.CopyrightEndingYear} Carina Studio";
		}) ?? "";
		this.Message = app?.GetStringNonNull("SplashWindow.Launching") ?? "";
		this.showAction = new(() =>
		{
			// get screen info
			var screen = this.Screens.ScreenFromWindow(this);
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
			var versionOpacity = this.FindResourceOrDefault("Double/ApplicationInfoDialog.AppVersion.Opacity", 0.75);
			((Control)this.Content.AsNonNull()).Opacity = 1;
			this.Get<Control>("colorBar").Let(control =>
			{
				(control.RenderTransform as ScaleTransform)?.Let(it => it.ScaleY = 1);
			});
			this.Get<Control>("titleTextBlock").Let(control =>
			{
				control.Opacity = 1;
				(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
			});
			this.Get<Control>("versionTextBlock").Let(control =>
			{
				control.Opacity = versionOpacity;
				(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
			});
			this.Get<Control>("copyrightTextBlock").Let(control =>
			{
				control.Opacity = versionOpacity;
				(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
			});
			this.Get<Control>("messageContainer").Let(control =>
			{
				control.Opacity = 1;
				(control.RenderTransform as TranslateTransform)?.Let(it => it.X = 0);
				control.PropertyChanged += (_, e) =>
				{
					if (e.Property == OpacityProperty && Math.Abs(1 - (double)e.NewValue!) <= double.Epsilon * 2)
					{
						this.ActivateAndBringToFront();
						this.RequestAnimationFrame(_ => this.initAnimationTaskCompletionSource.TrySetResult());
					}
				};
			});
		});
		this.Version = app?.GetFormattedString("ApplicationInfoDialog.Version", app.Assembly.GetName().Version).AsNonNull() ?? "";
		if (app?.ReleasingType != ApplicationReleasingType.Stable)
			this.Version += $" {AppReleasingTypeConverter.Convert<string?>(app?.ReleasingType)}";
		AvaloniaXamlLoader.Load(this);
		this.progressBar = this.Get<ProgressBar>(nameof(progressBar));
		if (Platform.IsWindows)
		{
			if (!Platform.IsWindows8OrAbove
			    || (IAppSuiteApplication.CurrentOrNull as AppSuiteApplication)?.AllowTransparentWindows == false)
			{
				this.Get<Panel>("rootPanel").Margin = default;
				this.Get<Border>("backgroundBorder").Let(it =>
				{
					it.BoxShadow = default;
					it.CornerRadius = default;
				});
			}
			this.Styles.Add((Avalonia.Styling.IStyle)this.Resources["windowsTheme"].AsNonNull());
		}
		else if (Platform.IsMacOS)
			this.Styles.Add((Avalonia.Styling.IStyle)this.Resources["macOSTheme"].AsNonNull());
		else if (Platform.IsLinux)
			this.Styles.Add((Avalonia.Styling.IStyle)this.Resources["linuxTheme"].AsNonNull());
		this.Styles.Add((Avalonia.Styling.IStyle)(app?.EffectiveThemeMode switch
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
			this.SetAndRaise(AccentColorProperty, ref this.accentColor, value);
			this.progressBar.Foreground = new SolidColorBrush(value);
		}
	}


	// Name of application.
	public string ApplicationName { get; }


	// Background image.
	public IImage? BackgroundImage => this.GetValue(BackgroundImageProperty);


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
			this.SetAndRaise(BackgroundImageOpacityProperty, ref this.backgroundImageOpacity, value);
		}
	}


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
				this.SetValue(BackgroundImageProperty, AssetLoader.Open(value).Use(stream => 
					new Bitmap(stream)));
			}
			else
				this.SetValue(BackgroundImageProperty, null);
		}
	}


	// Copyright.
	public string Copyright { get; }


	// Get icon as IBitmap.
	public Bitmap? IconBitmap => this.GetValue(IconBitmapProperty);


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
			this.Icon = AssetLoader.Open(value).Use(stream =>
				new WindowIcon(stream));
			this.SetValue(IconBitmapProperty, AssetLoader.Open(value).Use(stream =>
				new Bitmap(stream)));
		}
    }


	/// <summary>
	/// Get or set message to show.
	/// </summary>
	public string Message
	{
		get => this.GetValue(MessageProperty);
		set => this.SetValue(MessageProperty, value);
	}
	
	
	/// <summary>
	/// Get or set color of message to show.
	/// </summary>
	public Color MessageColor
	{
		get => this.GetValue(MessageColorProperty);
		set => this.SetValue(MessageColorProperty, value);
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
		
		// bring to front
		this.ActivateAndBringToFront();
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
				this.progressAnimator = new DoubleRenderingAnimator(this, this.progressBar.Value, Math.Max(0, Math.Min(1, value))).Also(it =>
				{
					it.Completed += async (_, _) => 
					{
						this.progressBar.Value = it.EndValue;
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
					};
					it.Duration = TimeSpan.FromMilliseconds(Math.Abs(it.StartValue - it.EndValue) * 500);
					it.Interpolator = Interpolators.SlowDeceleration;
					it.ProgressChanged += (_, _) => this.progressBar.Value = it.Value;
					it.Start();
				});
			}
		}
	}


	/// <inheritdoc/>
	public override void Render(DrawingContext context)
	{
		base.Render(context);
		if (this.renderingTaskSource != null)
		{
			Dispatcher.UIThread.Post(() =>
			{
				if (this.renderingTaskSource != null)
				{
					this.renderingTaskSource.TrySetResult();
					this.renderingTaskSource = null;
				}
			}, DispatcherPriority.Render);
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


	// Wait for initial animation.
	public Task WaitForInitialAnimationAsync() =>
		this.initAnimationTaskCompletionSource.Task;


	// Wait for completion of next rendering.
	public Task WaitForRenderingAsync()
	{
		if (this.renderingTaskSource == null)
		{
			this.renderingTaskSource = new();
			this.InvalidateVisual();
		}
		return this.renderingTaskSource.Task;
	}
}
