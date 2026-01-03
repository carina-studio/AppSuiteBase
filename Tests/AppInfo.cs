using Avalonia.Media;
using System;
using System.Collections.Generic;

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
    }

    public override IList<IImage> Badges { get; }

    public override Uri GitHubProjectUri => new("http://localhost/");

    public override IList<string> Products =>
    [
        "test1",
        "test2"
    ];

    public override Uri WebsiteUri => new("https://carinastudio.azurewebsites.net/");
}