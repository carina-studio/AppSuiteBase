using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Collections;
using CarinaStudio.Data.Converters;
using CarinaStudio.Threading;
using System;

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
		static readonly AvaloniaProperty<bool> HasGitHubProjectProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasGitHubProject));
		static readonly AvaloniaProperty<bool> HasPrivacyPolicyProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasPrivacyPolicy));
		static readonly AvaloniaProperty<bool> HasUserAgreementProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, bool>(nameof(HasUserAgreement));
		static readonly AvaloniaProperty<string?> VersionStringProperty = AvaloniaProperty.Register<ApplicationInfoDialogImpl, string?>(nameof(VersionString));


		// Constructor.
		public ApplicationInfoDialogImpl()
		{
			InitializeComponent();
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
					filter.Extensions.Add("txt");
					filter.Name = this.Application.GetString("FileFormat.Text");
				}));
				it.InitialFileName = $"Logs-{dateTime.ToString("yyyyMMdd-HHmmss")}.txt";
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
					Icon = MessageDialogIcon.Information,
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


		// Check whether GitHub exists or not.
		public bool HasGitHubProject { get => this.GetValue<bool>(HasGitHubProjectProperty); }


		// Check whether Privacy Policy exists or not.
		public bool HasPrivacyPolicy { get => this.GetValue<bool>(HasPrivacyPolicyProperty); }


		// Check whether User Agreement exists or not.
		public bool HasUserAgreement { get => this.GetValue<bool>(HasUserAgreementProperty); }


		// Initialize.
		private void InitializeComponent() => AvaloniaXamlLoader.Load(this);


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
					this.SetValue<bool>(HasGitHubProjectProperty, appInfo.GitHubProjectUri != null);
					this.SetValue<bool>(HasPrivacyPolicyProperty, appInfo.PrivacyPolicyUri != null);
					this.SetValue<bool>(HasUserAgreementProperty, appInfo.UserAgreementUri != null);
					this.SetValue<string?>(VersionStringProperty, Global.Run(() =>
					{
						var str = this.Application.GetFormattedString("ApplicationInfoDialog.Version", appInfo.Version);
						if (appInfo.ReleasingType == ApplicationReleasingType.Stable)
							return str;
						return str + $" ({AppReleasingTypeConverter.Convert<string?>(appInfo.ReleasingType)})";
					}));

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


        // String represent version.
        string? VersionString { get => this.GetValue<string?>(VersionStringProperty); }
	}
}