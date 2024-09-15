using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Base class of information of external dependency of application.
/// </summary>
public abstract class ExternalDependency : BaseApplicationObject<IAppSuiteApplication>, INotifyPropertyChanged
{
    // Fields.
    Task? availabilityCheckTask;
    readonly ScheduledAction checkAvailabilityAction;
    string? description;
    bool isFirstAvailabilityCheck = true;
    bool isDescriptionValid;
    ILogger? logger;
    string? name;
    ExternalDependencyState state = ExternalDependencyState.Unknown;


    /// <summary>
    /// Initialize new <see cref="ExternalDependency"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="id">Unique ID of dependency.</param>
    /// <param name="type">Type of dependency.</param>
    /// <param name="priority">Priority of dependency.</param>
    protected ExternalDependency(IAppSuiteApplication app, string id, ExternalDependencyType type, ExternalDependencyPriority priority) : base(app)
    {
        // ReSharper disable once AsyncVoidLambda
        this.checkAvailabilityAction = new(async () =>
        {
            if (this.state == ExternalDependencyState.CheckingForAvailability)
                return;
            this.availabilityCheckTask = this.CheckForAvailabilityAsync();
            await this.availabilityCheckTask;
            this.availabilityCheckTask = null;
        });
        this.Id = id;
        this.Priority = priority;
        this.Type = type;
        app.AddWeakEventHandler(nameof(IApplication.StringsUpdated), this.OnAppStringsUpdated);
        this.InvalidateAvailability();
    }


    // Check for availability asynchronously.
    async Task CheckForAvailabilityAsync()
    {
        if (this.state == ExternalDependencyState.CheckingForAvailability)
            return;
        this.State = ExternalDependencyState.CheckingForAvailability;
        if (!this.isFirstAvailabilityCheck)
            await Task.Delay(500);
        else
            this.isFirstAvailabilityCheck = false;
        try
        {
            Logger.LogDebug("Check availability");
            this.State = await this.OnCheckAvailabilityAsync()
                ? ExternalDependencyState.Available
                : ExternalDependencyState.Unavailable;
        }
        catch
        { 
            this.State = ExternalDependencyState.Unavailable;
        }
    }


    /// <summary>
    /// Get description of dependency.
    /// </summary>
    public string? Description
    {
        get
        {
            if (!this.isDescriptionValid)
            {
                this.description = this.OnUpdateDescription();
                this.isDescriptionValid = true;
            }
            return this.description;
        }
    }


    /// <summary>
    /// Get URI for details of external dependency.
    /// </summary>
    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public virtual Uri? DetailsUri { get; }


    /// <summary>
    /// Get unique ID of dependency.
    /// </summary>
    public string Id { get; }


    /// <summary>
    /// Get URI for downloading and installation of external dependency.
    /// </summary>
    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public virtual Uri? InstallationUri { get; }


    /// <summary>
    /// Invalidate and check availability of external dependency.
    /// </summary>
    public void InvalidateAvailability() =>
        this.checkAvailabilityAction.Schedule();


    /// <summary>
    /// Get logger.
    /// </summary>
    protected ILogger Logger
    {
        get
        {
            logger ??= this.Application.LoggerFactory.CreateLogger(this.GetType().Name);
            return logger;
        }
    }


    /// <summary>
    /// Get name of dependency.
    /// </summary>
    public string Name
    {
        get
        {
            this.name ??= this.OnUpdateName();
            return this.name;
        }
    }


    // Called when application strings updated.
    void OnAppStringsUpdated(object? sender, EventArgs e)
    {
        if (this.isDescriptionValid)
        {
            this.isDescriptionValid = false;
            this.description = null;
            this.PropertyChanged?.Invoke(this, new(nameof(Description)));
        }
        if (this.name != null)
        {
            this.name = null;
            this.PropertyChanged?.Invoke(this, new(nameof(Name)));
        }
    }


    /// <summary>
    /// Called to check whether external dependency is available or not.
    /// </summary>
    /// <returns>Task of checking availability.</returns>
    protected abstract Task<bool> OnCheckAvailabilityAsync();


    /// <summary>
    /// Called to update description of dependency.
    /// </summary>
    /// <returns>Description of dependency.</returns>
    protected virtual string? OnUpdateDescription() =>
        this.Application.GetString($"ExternalDependency.{this.Id}.Description");


    /// <summary>
    /// Called to update name of dependency.
    /// </summary>
    /// <returns>Name of dependency.</returns>
    protected virtual string OnUpdateName() =>
        this.Application.GetStringNonNull($"ExternalDependency.{this.Id}", this.Id);
    

    /// <summary>
    /// Get priority of external dependency.
    /// </summary>
    public ExternalDependencyPriority Priority { get; }


    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;


     /// <summary>
    /// Get state of external dependency.
    /// </summary>
    public ExternalDependencyState State 
    { 
        get => this.state;
        private set
        {
            if (this.state == value)
                return;
            this.state = value;
            this.PropertyChanged?.Invoke(this, new(nameof(State)));
        }
    }


    /// <summary>
    /// Get type of external dependency.
    /// </summary>
    public ExternalDependencyType Type { get; }


    /// <summary>
    /// Wait for availability check completed.
    /// </summary>
    /// <returns>Task of waiting.</returns>
    public Task WaitForCheckingAvailability()
    {
        this.VerifyAccess();
        return this.availabilityCheckTask ?? Task.CompletedTask;
    }
}


/// <summary>
/// Priority of external dependency of application.
/// </summary>
public enum ExternalDependencyPriority
{
    /// <summary>
    /// Required to run application.
    /// </summary>
    Required,
    /// <summary>
    /// Required by some of features of application.
    /// </summary>
    RequiredByFeatures,
    /// <summary>
    /// Required (optional) by some of features of application.
    /// </summary>
    Optional,
}


/// <summary>
/// State of external dependency of application.
/// </summary>
public enum ExternalDependencyState
{
    /// <summary>
    /// Unknown.
    /// </summary>
    Unknown,
    /// <summary>
    /// Checking for availability.
    /// </summary>
    CheckingForAvailability,
    /// <summary>
    /// Available.
    /// </summary>
    Available,
    /// <summary>
    /// Unavailable.
    /// </summary>
    Unavailable,
}


/// <summary>
/// Type of external dependency.
/// </summary>
public enum ExternalDependencyType
{
    /// <summary>
    /// Software.
    /// </summary>
    Software,
    /// <summary>
    /// Configuration.
    /// </summary>
    Configuration,
    /// <summary>
    /// Permission.
    /// </summary>
    Permission,
}