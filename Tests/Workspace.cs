using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Tests
{
    class Workspace : ViewModels.MainWindowViewModel<App>
    {
        protected override string? OnUpdateTitle()
        {
            return "AppSuite Test";
        }
    }
}
