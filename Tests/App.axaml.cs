using Avalonia;
using Avalonia.Markup.Xaml;
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

        public override int DefaultLogOutputTargetPort => 5566;

        public override IEnumerable<ExternalDependency> ExternalDependencies { get => this.externalDependencies; }

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
            this.ShowMainWindow();
        }


        protected override async Task OnPrepareStartingAsync()
        {
            this.externalDependencies.Add(new ExecutableExternalDependency(this, "dotnet", ExternalDependencyPriority.Required, "dotnet", new Uri("https://dotnet.microsoft.com/"), new Uri("https://dotnet.microsoft.com/download")));
            
            await base.OnPrepareStartingAsync();

            if (!this.IsRestoringMainWindowsRequested)
            {
                for (var i = 0; i < 1; ++i)
                    this.ShowMainWindow();
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


        //public override Uri? PackageManifestUri => new Uri("https://raw.githubusercontent.com/carina-studio/ULogViewer/master/PackageManifest-Preview.json");


        public override Version? PrivacyPolicyVersion => new Version(1, 2);


        public override Version? UserAgreementVersion => new Version(1, 3);
    }
}
