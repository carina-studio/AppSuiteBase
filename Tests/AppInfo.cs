using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;

namespace CarinaStudio.AppSuite.Tests
{
    class AppInfo : ViewModels.ApplicationInfo
    {
        public override Uri? GitHubProjectUri => new Uri("http://localhost/");

        public override Uri? PrivacyPolicyUri => new Uri("http://localhost/");

        public override Uri? UserAgreementUri => new Uri("http://localhost/");
    }
}
