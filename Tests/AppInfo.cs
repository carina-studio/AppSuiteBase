using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace CarinaStudio.AppSuite.Tests;

class AppInfo : ViewModels.ApplicationInfo
{
    public AppInfo()
    {
        this.Application.TryFindResource<IImage>("Image/Icon.Professional.Colored.Gradient", out var badge1);
        this.Application.TryFindResource<IImage>("Image/Icon.Website", out var badge2);
        this.Badges = [
            badge1.AsNonNull(),
            badge2.AsNonNull(),
        ];
        this.BannerImage = AssetLoader.Open(new($"avares://{Assembly.GetEntryAssembly()!.GetName().Name}/ApplicationInfoBanner.jpg")).Use(stream => new Bitmap(stream));
    }

    public override IList<IImage> Badges { get; }

    public override IImage? BannerImage { get; }

    public override Uri GitHubProjectUri => new("http://localhost/");

    public override IList<string> Products =>
    [
        "test1",
        "test2"
    ];

    public override Uri WebsiteUri => new("https://carinastudio.azurewebsites.net/");
}