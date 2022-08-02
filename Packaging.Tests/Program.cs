namespace CarinaStudio.AppSuite.Packaging;

static class Program
{
    static int Main(string[] args) =>
        (int)new PackagingTool().Run(args);
}