using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using System;

namespace CarinaStudio.AppSuite.Tests
{
    class AppInfo : ViewModels.ApplicationInfo
    {
        public override WindowIcon Icon { get; } = AvaloniaLocator.Current.GetService<IAssetLoader>().Let(it =>
        {
            return it.Open(new Uri("avares://CarinaStudio.AppSuite.Tests/AppIcon.ico")).Use(stream => new WindowIcon(stream));
        });
    }
}
