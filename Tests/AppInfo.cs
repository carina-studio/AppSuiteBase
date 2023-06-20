using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CarinaStudio.Controls;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Tests
{
    class AppInfo : ViewModels.ApplicationInfo
    {
        public AppInfo()
        {
            this.Application.TryFindResource<IImage>("Image/Icon.Professional.Colored", out var badge1);
            this.Application.TryFindResource<IImage>("Image/Icon.Website", out var badge2);
            this.Badges = new IImage[] {
                badge1.AsNonNull(),
                badge2.AsNonNull(),
            };
        }

        public override IList<IImage> Badges { get; }

        public override Uri? GitHubProjectUri => new Uri("http://localhost/");

        public override IList<string> Products => new string[] {
            "test1",
            "test2"
        };

        public override Uri? WebsiteUri => new Uri("https://carinastudio.azurewebsites.net/");
    }
}
