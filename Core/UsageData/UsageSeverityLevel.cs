namespace CarinaStudio.AppSuite.UsageData;

/// <summary>
/// Severity level for <see cref="IUsageManager.TrackException"/>.
/// </summary>
public enum UsageSeverityLevel
{
    /// <summary>Verbose / informational.</summary>
    Verbose,
    /// <summary>Warning — unexpected but recoverable.</summary>
    Warning,
    /// <summary>Error — operation failed.</summary>
    Error,
    /// <summary>Critical — application stability is at risk.</summary>
    Critical,
}
