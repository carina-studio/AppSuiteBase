using Avalonia;
using Avalonia.Markup.Xaml;
using CarinaStudio.Controls;
using CarinaStudio.ViewModels;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Tests
{
    public class App : AppSuiteApplication<App>
    {
        // Avalonia configuration, don't remove; also used by visual designer.
        static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();


        protected override bool AllowMultipleMainWindows => true;


        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }


        static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }


        protected override Window<App> OnCreateMainWindow(object? param) => new MainWindow();


        protected override ViewModel<App> OnCreateMainWindowViewModel(object? param) => new Workspace();


        protected override async Task OnPrepareStartingAsync()
        {
            await base.OnPrepareStartingAsync();
            for (var i = 0; i < 1; ++i)
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
    }
}
