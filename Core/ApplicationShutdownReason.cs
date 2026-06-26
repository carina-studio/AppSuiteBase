namespace CarinaStudio.AppSuite;

/// <summary>
/// Reason of shutting down application.
/// </summary>
public enum ApplicationShutdownReason
{
    /// <summary>
    /// Shutting down without specific reason.
    /// </summary>
    None,
    /// <summary>
    /// Shutting down for critical reason (e.g. forced by system). Asynchronous preparation before shutting down is not allowed.
    /// </summary>
    Critical,
    /// <summary>
    /// Shutting down to update application.
    /// </summary>
    UpdatingApplication,
}
