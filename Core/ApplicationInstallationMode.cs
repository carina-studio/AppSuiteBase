namespace CarinaStudio.AppSuite;

/// <summary>
/// Mode of how the application should be installed on device.
/// </summary>
public enum ApplicationInstallationMode
{
    /// <summary>
    /// Default.
    /// </summary>
    Default,
    /// <summary>
    /// Installed and managed by package manager.
    /// </summary>
    PackageManager,
}