using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.Tests
{
    class AppInfo : ViewModels.ApplicationInfo
    {
        public override Uri? GitHubProjectUri => new Uri("http://localhost/");

        public override string GetProductName(string productId)
        {
            if (productId == "test1")
                return "Test Product 1";
            if (productId == "test2")
                return "Test Product 2";
            return base.GetProductName(productId);
        }

        public override Uri? PrivacyPolicyUri => new Uri("https://carina-studio.github.io/ULogViewer/privacy_policy.html");

        public override IList<string> Products => new string[] {
            "test1",
            "test2"
        };

        public override Uri? UserAgreementUri => new Uri("https://carina-studio.github.io/ULogViewer/user_agreement.html");
    }
}
