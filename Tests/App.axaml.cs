using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Tests
{
    public class App : AppSuiteApplication
    {
        // Avalonia configuration, don't remove; also used by visual designer.
        static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();


        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }


        static void Main(string[] args)
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }


        protected override Window OnCreateMainWindow(object? param) => new MainWindow();


        protected override async Task OnPrepareStartingAsync()
        {
            await base.OnPrepareStartingAsync();
            this.ShowMainWindow();
        }
    }
}
