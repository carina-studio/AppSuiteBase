using Avalonia;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Collections;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Tests
{
    public class App : AppSuiteApplication
    {
        readonly List<ExternalDependency> externalDependencies = new();

        protected override bool AllowMultipleMainWindows => true;

        public override ApplicationInfo CreateApplicationInfoViewModel() => new AppInfo();

        public override ApplicationOptions CreateApplicationOptionsViewModel() => new ApplicationOptions();

        public override int DefaultLogOutputTargetPort => 5566;

        public override IEnumerable<ExternalDependency> ExternalDependencies { get => this.externalDependencies; }

        public override int ExternalDependenciesVersion { get; } = 2;

        public override void Initialize()
        {
            this.Name = "AppSuite";
            AvaloniaXamlLoader.Load(this);
        }


        [STAThread]
        static void Main(string[] args)
        {
            BuildApplication<App>().StartWithClassicDesktopLifetime(args);
        }


        protected override Window OnCreateMainWindow() => new MainWindow();


        protected override ViewModel OnCreateMainWindowViewModel(JsonElement? savedState) => new Workspace(savedState);


        protected override void OnNewInstanceLaunched(IDictionary<string, object> launchOptions)
        {
            base.OnNewInstanceLaunched(launchOptions);
            _ = this.ShowMainWindowAsync();
        }


        protected override Controls.SplashWindowParams OnPrepareSplashWindow() => base.OnPrepareSplashWindow().Also((ref Controls.SplashWindowParams param) =>
        {
            param.AccentColor = Avalonia.Media.Color.FromArgb(0xff, 0x91, 0x2f, 0xbf);
            //param.BackgroundImageUri = null;
        });


        protected override async Task OnPrepareStartingAsync()
        {
            this.externalDependencies.Add(new ExecutableExternalDependency(this, "dotnet", ExternalDependencyPriority.Optional, "dotnet", new Uri("https://dotnet.microsoft.com/"), new Uri("https://dotnet.microsoft.com/download")));
            this.externalDependencies.Add(new ExecutableExternalDependency(this, "bash", ExternalDependencyPriority.RequiredByFeatures, "bash", null, new Uri("https://www.gnu.org/software/bash/")));
            if (Platform.IsLinux)
				this.externalDependencies.Add(new ExecutableExternalDependency(this, "XRandR", ExternalDependencyPriority.Optional, "xrandr", new Uri("https://www.x.org/wiki/Projects/XRandR/"), new Uri("https://command-not-found.com/xrandr")));
            
            await base.OnPrepareStartingAsync();

            this.UpdateSplashWindowProgress(0.5);
            await this.WaitForSplashWindowAnimationAsync();

            if (!this.IsRestoringMainWindowsRequested)
            {
                for (var i = 0; i < 1; ++i)
                    await this.ShowMainWindowAsync();
            }
        }


        protected override bool OnSelectEnteringDebugMode()
        {
#if DEBUG
            return true;
#else
            return base.OnSelectEnteringDebugMode();
#endif
        }


        protected override bool OnTryExitingBackgroundMode()
        {
            if (base.OnTryExitingBackgroundMode())
                return true;
            _ = this.ShowMainWindowAsync();
            return true;
        }


        //public override Uri? PackageManifestUri => new Uri("https://raw.githubusercontent.com/carina-studio/ULogViewer/master/PackageManifest-Preview.json");


        public override Version? PrivacyPolicyVersion => new Version(1, 3);


        //protected override string? ProVersionProductId => "Test";


        public override Task ShowApplicationOptionsDialogAsync(Avalonia.Controls.Window? owner, string? section = null) =>
            Task.CompletedTask;


        public override Version? UserAgreementVersion => new Version(1, 5);
    }
}
