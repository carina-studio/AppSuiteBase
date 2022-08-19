using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Product;

/// <summary>
/// Interface of product manager.
/// </summary>
public interface IProductManager : IApplicationObject<IAppSuiteApplication>, INotifyPropertyChanged
{
    /// <summary>
    /// Add new product to be track by manager.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="authCode">Authorization code of product.</param>
    /// <param name="emailAddress">E-mail address to own product.</param>
    void AddProduct(string id, string authCode, string emailAddress);


    /// <summary>
    /// Show related UI to activate given product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="window">Window to show dialog.</param>
    /// <returns>Task of activating product.</returns>
    Task ActivateProductAsync(string id, Avalonia.Controls.Window window);


    /// <summary>
    /// Check whether at least one product need to be activated online or not.
    /// </summary>
    bool HasPendingOnlineProductActivation { get; }


    /// <summary>
    /// Check whether specific product is tracking by manager or not.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <returns>True if product is tracking by manager.</returns>
    bool HasProduct(string id);


    /// <summary>
    /// Check whether at least one product online activation is on-going or not.
    /// </summary>
    bool IsActivatingProductsOnline { get; }


    /// <summary>
    /// Check whether the instance is mock implementation or not.
    /// </summary>
    bool IsMock { get; }


    /// <summary>
    /// Check whether specific product has been activated or not.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="onlineActivationNeeded">True if online activation should be take into account.</param>
    /// <returns>True if product has been activated.</returns>
    bool IsProductActivated(string id, bool onlineActivationNeeded = false);


    /// <summary>
    /// Get all ID of products which are being tracked by manager.
    /// </summary>
    IList<string> Products { get; }


    /// <summary>
    /// Raised when activation state of product has changed.
    /// </summary>
    event Action<IProductManager, string, bool> ProductActivationChanged;


    /// <summary>
    /// Raised when state of product has changed.
    /// </summary>
    event Action<IProductManager, string>? ProductStateChanged;


    /// <summary>
    /// Show UI to purchase given product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="window">Window for showing UI.</param>
    void PurchaseProduct(string id, Avalonia.Controls.Window window);


    /// <summary>
    /// Try getting last known reason of failure of specific product activation.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="failure">Failure reason.</param>
    /// <returns>True if failure reason got successfully.</returns>
    bool TryGetProductActivationFailure(string id, out ProductActivationFailure failure);


    /// <summary>
    /// Try getting active devices of specified product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="devices">Active devices.</param>
    /// <returns>True if devices got successfully.</returns>
    bool TryGetProductActiveDevices(string id, out IActiveDeviceInfo[] devices);


    /// <summary>
    /// Try getting description of specific product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="description">Description of product.</param>
    /// <returns>True if description got successfully.</returns>
    bool TryGetProductDescription(string id, out string description);


    /// <summary>
    /// Try getting description of specific product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="description">Description of product.</param>
    /// <returns>True if description got successfully.</returns>
    bool TryGetProductDescription(string id, out IObservable<string> description);


    /// <summary>
    /// Try getting e-mail address of specific product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="emailAddress">E-mail address.</param>
    /// <returns>True if e-mail address got successfully.</returns>
    bool TryGetProductEmailAddress(string id, out string emailAddress);


    /// <summary>
    /// Try getting maximum number of allowed devices of specific product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="deviceCount">Maximum number of allowed devices.</param>
    /// <returns>True if maximum number of allowed devices got successfully.</returns>
    bool TryGetProductMaxDeviceCount(string id, out int deviceCount);


    /// <summary>
    /// Try getting display name of specific product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="name">Name of product.</param>
    /// <returns>True if name got successfully.</returns>
    bool TryGetProductName(string id, out string name);


    /// <summary>
    /// Try getting display name of specific product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="name">Name of product.</param>
    /// <returns>True if name got successfully.</returns>
    bool TryGetProductName(string id, out IObservable<string> name);


    /// <summary>
    /// Try getting state of specified product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="state">Product state.</param>
    /// <returns>True if state got successfully.</returns>
    bool TryGetProductState(string id, out ProductState state);


    /// <summary>
    /// Wait for specific product to be activated online.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task or waiting.</returns>
    Task WaitForOnlineProductActivation(string id, CancellationToken cancellationToken = default);
}


// Mock implementation of IProductManager.
class MockProductManager : BaseApplicationObject<IAppSuiteApplication>, IProductManager
{
    // Constructor.
    public MockProductManager(IAppSuiteApplication app) : base(app)
    { }


    /// <inheritdoc/>
    public Task ActivateProductAsync(string id, Avalonia.Controls.Window window) => Task.CompletedTask;


    /// <inheritdoc/>
    public void AddProduct(string id, string authCode, string emailAddress)
    { }


    /// <summary>
    /// Check whether at least one product need to be activated online or not.
    /// </summary>
    public bool HasPendingOnlineProductActivation { get; }


    /// <inheritdoc/>
    public bool HasProduct(string id) => false;


    /// <inheritdoc/>
    public bool IsActivatingProductsOnline { get; }


    /// <inheritdoc/>
    public bool IsMock { get => true; }


    /// <inheritdoc/>
    public bool IsProductActivated(string id, bool onlineActivationNeeded = false) => false;


    /// <inheritdoc/>
    public IList<string> Products { get; } = new string[0];


    /// <inheritdoc/>
    event Action<IProductManager, string, bool>? IProductManager.ProductActivationChanged
    {
        add
        { }
        remove
        { }
    }


    /// <inheritdoc/>
    event Action<IProductManager, string>? IProductManager.ProductStateChanged
    {
        add
        { }
        remove
        { }
    }


    /// <inheritdoc/>
    event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
    {
        add
        { }
        remove
        { }
    }


    /// <inheritdoc/>
    public void PurchaseProduct(string id, Avalonia.Controls.Window window)
    { }


    /// <inheritdoc/>
    public bool TryGetProductActivationFailure(string id, out ProductActivationFailure failure)
    {
        failure = default;
        return false;
    }


    /// <inheritdoc/>
    public bool TryGetProductDescription(string id, out string description)
    {
        description = "";
        return false;
    }


    /// <inheritdoc/>
    public bool TryGetProductDescription(string id, out IObservable<string> description)
    {
        description = new FixedObservableValue<string>("");
        return false;
    }


    /// <summary>
    /// Try getting active devices of specified product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="devices">Active devices.</param>
    /// <returns>True if devices got successfully.</returns>
    public bool TryGetProductActiveDevices(string id, out IActiveDeviceInfo[] devices)
    {
        devices = new IActiveDeviceInfo[0];
        return false;
    }


    /// <summary>
    /// Try getting e-mail address of specific product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="emailAddress">E-mail address.</param>
    /// <returns>True if e-mail address got successfully.</returns>
    public bool TryGetProductEmailAddress(string id, out string emailAddress)
    {
        emailAddress = "";
        return false;
    }


    /// <summary>
    /// Try getting maximum number of allowed devices of specific product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="deviceCount">Maximum number of allowed devices.</param>
    /// <returns>True if maximum number of allowed devices got successfully.</returns>
    public bool TryGetProductMaxDeviceCount(string id, out int deviceCount)
    {
        deviceCount = default;
        return false;
    }


    /// <inheritdoc/>
    public bool TryGetProductName(string id, out string name)
    {
        name = "";
        return false;
    }


    /// <inheritdoc/>
    public bool TryGetProductName(string id, out IObservable<string> name)
    {
        name = new FixedObservableValue<string>("");
        return false;
    }


    /// <summary>
    /// Try getting state of specified product.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="state">Product state.</param>
    /// <returns>True if state got successfully.</returns>
    public bool TryGetProductState(string id, out ProductState state)
    {
        state = default;
        return false;
    }


    /// <summary>
    /// Wait for specific product to be activated online.
    /// </summary>
    /// <param name="id">ID of product.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task or waiting.</returns>
    public Task WaitForOnlineProductActivation(string id, CancellationToken cancellationToken = default) => Task.CompletedTask;
}