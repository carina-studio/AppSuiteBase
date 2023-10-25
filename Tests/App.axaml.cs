using Avalonia;
using Avalonia.Markup.Xaml;
using CarinaStudio.AppSuite.ViewModels;
using CarinaStudio.Collections;
using CarinaStudio.Controls;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Tests
{
    public class App : AppSuiteApplication
    {
        class AgreementDocumentSource : DocumentSource
        {
            public AgreementDocumentSource(IAppSuiteApplication app) : base(app)
            { }

            public override IList<ApplicationCulture> SupportedCultures => new ApplicationCulture[]
            {
                ApplicationCulture.EN_US,
                ApplicationCulture.ZH_TW
            };

            public override Uri Uri => this.Culture switch
            {
                ApplicationCulture.ZH_TW => new($"avares://{Assembly.GetExecutingAssembly().GetName().Name}/Document-zh-TW.md"),
                _ => new($"avares://{Assembly.GetExecutingAssembly().GetName().Name}/Document.md"),
            };
        }


        class AppChangeListDocumentSource : DocumentSource
        {
            public AppChangeListDocumentSource(IAppSuiteApplication app) : base(app)
            { }

            public override IList<ApplicationCulture> SupportedCultures => new ApplicationCulture[]
            {
                ApplicationCulture.EN_US,
                ApplicationCulture.ZH_CN,
                ApplicationCulture.ZH_TW,
            };

            public override Uri Uri => this.Culture switch
            {
                ApplicationCulture.ZH_CN => this.Application.CreateAvaloniaResourceUri("ChangeList-zh-CN.md"),
                ApplicationCulture.ZH_TW => this.Application.CreateAvaloniaResourceUri("ChangeList-zh-TW.md"),
                _ => this.Application.CreateAvaloniaResourceUri("ChangeList.md"),
            };
        }


        class DotNet7ExternalDependency : ExternalDependency
        {
            public DotNet7ExternalDependency(App app) : base(app, "dotnet7", ExternalDependencyType.Configuration, ExternalDependencyPriority.RequiredByFeatures)
            { }

            protected override async Task<bool> OnCheckAvailabilityAsync() => await Task.Run(() =>
            {
                var process = Process.Start(new ProcessStartInfo()
                {
                    Arguments = "--version",
                    CreateNoWindow = true,
                    FileName = "dotnet",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                });
                if (process != null)
                {
                    var isDotNet7 = (process.StandardOutput.ReadLine()?.Contains("7.0")).GetValueOrDefault();
                    process.WaitForExit();
                    return isDotNet7;
                }
                return false;
            }, CancellationToken.None);

            protected override string? OnUpdateDescription() =>
                "Description of .NET 7.";

            protected override string OnUpdateName() =>
                ".NET 7";
        }

        readonly List<ExternalDependency> externalDependencies = new();

        static App()
        {
            LogToConsole("Initialize App type");
        }

        public App()
        {
            LogToConsole("Initialize App instance");
        }

        protected override bool AllowMultipleMainWindows => true;


        public override DocumentSource? ChangeList => new AppChangeListDocumentSource(this);

        public override ApplicationInfo CreateApplicationInfoViewModel() => new AppInfo();

        public override ApplicationOptions CreateApplicationOptionsViewModel() => new();

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
            BuildApplicationAndStart<App>(args);
        }


        protected override Controls.MainWindow OnCreateMainWindow() => new MainWindow();


        protected override ViewModel OnCreateMainWindowViewModel(JsonElement? savedState) => new Workspace(savedState);


        //protected override bool OnExceptionOccurredInApplicationLifetime(Exception ex) => true;


        protected override void OnNewInstanceLaunched(IDictionary<string, object> launchOptions)
        {
            base.OnNewInstanceLaunched(launchOptions);
            _ = this.ShowMainWindowAsync();
        }


        protected override Controls.SplashWindowParams OnPrepareSplashWindow() => base.OnPrepareSplashWindow().Also((ref Controls.SplashWindowParams param) =>
        {
            param.AccentColor = Avalonia.Media.Color.Parse("#912fbf");
            //param.AccentColor = Avalonia.Media.Color.Parse("#45e5ad");
            //param.BackgroundImageUri = null;
        });


        protected override async Task OnPrepareStartingAsync()
        {
            LogToConsole("Prepare starting (App)");
            
            this.externalDependencies.Add(new DotNet7ExternalDependency(this));
            this.externalDependencies.Add(new ExecutableExternalDependency(this, "dotnet", ExternalDependencyPriority.Optional, "dotnet", new Uri("https://dotnet.microsoft.com/"), new Uri("https://dotnet.microsoft.com/download")));
            this.externalDependencies.Add(new ExecutableExternalDependency(this, "bash", ExternalDependencyPriority.RequiredByFeatures, "bash", null, new Uri("https://www.gnu.org/software/bash/")));
            if (Platform.IsLinux)
				this.externalDependencies.Add(new ExecutableExternalDependency(this, "XRandR", ExternalDependencyPriority.Optional, "xrandr", new Uri("https://www.x.org/wiki/Projects/XRandR/"), new Uri("https://command-not-found.com/xrandr")));
            
            await base.OnPrepareStartingAsync();
            if (this.IsShutdownStarted)
                return;
            this.UpdateSplashWindowProgress(0.4);

            await Controls.SyntaxHighlighting.InitializeAsync(this);
            await this.WaitForSplashWindowAnimationAsync();
            this.UpdateSplashWindowProgress(0.7);

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


        /*
        public override IEnumerable<Uri> PackageManifestUris => new[]
        {
            new Uri("https://raw.githubusercontent.com/carina-studio/PixelViewer/master/PackageManifest-Preview.json"),
            new Uri("https://raw.githubusercontent.com/carina-studio/ULogViewer/master/PackageManifest-Preview.json"),
        };
        */


        public override DocumentSource? PrivacyPolicy => new AgreementDocumentSource(this);


        public override Version? PrivacyPolicyVersion => new(1, 4);


        //protected override string? ProVersionProductId => "Test";


        public override Task ShowApplicationOptionsDialogAsync(Avalonia.Controls.Window? owner, string? section = null) =>
            Task.CompletedTask;


        public override DocumentSource? UserAgreement => new AgreementDocumentSource(this);


        public override Version? UserAgreementVersion => new(1, 6);
    }
}
