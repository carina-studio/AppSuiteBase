using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace CarinaStudio.AppSuite.Tests
{
    class AppInfo : ViewModels.ApplicationInfo
    {
        public override IBitmap Icon { get; } = AvaloniaLocator.Current.GetService<IAssetLoader>().Let(it =>
        {
            return it.Open(new Uri("avares://CarinaStudio.AppSuite.Tests/AppIcon.ico")).Use(stream => new Bitmap(stream));
        });
    }
}
