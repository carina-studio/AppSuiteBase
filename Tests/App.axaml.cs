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
        protected override bool AllowMultipleMainWindows => true;


        protected override bool ForceAcceptingUpdateInfo => true;


        public override void Initialize()
        {
            this.Name = "AppSuite";
            AvaloniaXamlLoader.Load(this);
        }


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
            await base.OnPrepareStartingAsync();

            if (!this.IsRestoringMainWindowsRequested)
                this.ShowMainWindow();
        }


        protected override bool OnSelectEnteringDebugMode()
        {
#if DEBUG
            return true;
#else
            return base.OnSelectEnteringDebugMode();
#endif
        }


        public override System.Uri? PackageManifestUri => new Uri("https://raw.githubusercontent.com/carina-studio/PixelViewer/master/PackageManifest-Preview.json");
    }
}
