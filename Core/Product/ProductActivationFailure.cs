namespace CarinaStudio.AppSuite.Product;

/// <summary>
/// Reason of failure of product activation.
/// </summary>
public enum ProductActivationFailure
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown,
    /// <summary>
    /// Network connection is unavailable.
    /// </summary>
    NoNetworkConnection,
    /// <summary>
    /// Product cannot be found.
    /// </summary>
    ProductNotFound,
    /// <summary>
    /// Maximum number of allowed devices reached.
    /// </summary>
    MaxDeviceCountReached,
    /// <summary>
    /// Server-side error.
    /// </summary>
    ServerError,
}