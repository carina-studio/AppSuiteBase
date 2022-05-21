using System;

namespace CarinaStudio.AppSuite.Product;

/// <summary>
/// Interface of information of device which activates the product.
/// </summary>
public interface IActiveDeviceInfo
{
    /// <summary>
    /// Get last activation time by the device.
    /// </summary>
    DateTime ActivationTime { get; }


    /// <summary>
    /// Get unique ID of device.
    /// </summary>
    string Id { get; }


    /// <summary>
    /// Check whether the device is current device or not.
    /// </summary>
    bool IsCurrentDevice { get; }


    /// <summary>
    /// Get name of device.
    /// </summary>
    string Name { get; }
}