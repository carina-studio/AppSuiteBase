using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// Splash window when launching application.
	/// </summary>
	partial class SplashWindowImpl : Avalonia.Controls.Window
	{
		// Static fields.
		static readonly AvaloniaProperty<IBitmap?> IconBitmapProperty = AvaloniaProperty.Register<SplashWindowImpl, IBitmap?>(nameof(IconBitmap));
		static readonly AvaloniaProperty<string> MessageProperty = AvaloniaProperty.Register<SplashWindowImpl, string>(nameof(Message), " ", coerce: ((_, it) => string.IsNullOrEmpty(it) ? " " : it));


		/// <summary>
		/// Initialize new <see cref="SplashWindowImpl"/>.
		/// </summary>
		public SplashWindowImpl()
		{
			var app = AppSuiteApplication.Current;
			this.ApplicationName = app.Name ?? "";
			this.Message = app.GetStringNonNull("SplashWindow.Launching");
			this.Version = app.GetFormattedString("ApplicationInfoDialog.Version", app.Assembly.GetName().Version).AsNonNull();
			InitializeComponent();
		}


		// Name of application.
		public string ApplicationName { get; }


		// Get icon as IBitmap.
		public IBitmap? IconBitmap { get => this.GetValue<IBitmap?>(IconBitmapProperty); }


		// Get or set URI of application icon.
		public Uri? IconUri { get; set; }


		// Initialize.
		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


		/// <summary>
		/// Get or set message to show.
		/// </summary>
		public string Message
		{
			get => this.GetValue<string>(MessageProperty);
			set => this.SetValue<string>(MessageProperty, value);
		}


		// Called when opened.
		protected override void OnOpened(EventArgs e)
		{
			// call base
			base.OnOpened(e);

			// load icon
			var iconUri = this.IconUri ?? new Uri($"avares://{AppSuiteApplication.Current.Assembly.GetName()}/AppIcon.ico");
			this.SetValue<IBitmap?>(IconBitmapProperty, AvaloniaLocator.Current.GetService<IAssetLoader>().Let(loader =>
			{
				return loader.Open(iconUri).Use(stream => new Bitmap(stream));
			}));

			// move to center of screen
			var screen = this.Screens.ScreenFromVisual(this);
			var screenBounds = screen.Bounds;
			var pixelDensity = screen.PixelDensity;
			var width = this.Width;
			var height = this.Height;
			if (!Platform.IsMacOS)
			{
				width *= pixelDensity;
				height *= pixelDensity;
			}
			this.Position = new PixelPoint((int)((screenBounds.Width - width) / 2), (int)((screenBounds.Height - height) / 2));

            // show content
            ((Control)(this.Content)).Opacity = 1;
        }


        // String represents version.
        string Version { get; }
	}
}
