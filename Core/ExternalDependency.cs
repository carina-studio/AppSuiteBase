using CarinaStudio.Threading;
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
    readonly ScheduledAction checkAvailabilityAction;
    string? description;
    bool isDescriptionValid;
    string? name;
    ExternalDependencyState state = ExternalDependencyState.Unknown;


    /// <summary>
    /// Initialize new <see cref="ExternalDependency"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="id">Unique ID of dependency.</param>
    /// <param name="priority">Priority of dependency.</param>
    protected ExternalDependency(IAppSuiteApplication app, string id, ExternalDependencyPriority priority) : base(app)
    {
        this.checkAvailabilityAction = new(async () =>
        {
            if (this.state == ExternalDependencyState.CheckingForAvailability)
                return;
            this.State = ExternalDependencyState.CheckingForAvailability;
            try
            {
                this.State = (await this.OnCheckAvailabilityAsync())
                    ? ExternalDependencyState.Available
                    : ExternalDependencyState.Unavailable;
            }
            catch
            { 
                this.State = ExternalDependencyState.Unavailable;
            }
        });
        this.Id = id;
        this.Priority = priority;
        app.AddWeakEventHandler(nameof(IApplication.StringsUpdated), this.OnAppStringsUpdated);
        this.InvalidateAvailability();
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
    public virtual Uri? DetailsUri { get; }


    /// <summary>
    /// Get unique ID of dependency.
    /// </summary>
    public string Id { get; }


    /// <summary>
    /// Get URI for downloading and installation of external dependency.
    /// </summary>
    public virtual Uri? InstallationUri { get; }


    /// <summary>
    /// Invalidate and check availability of external dependency.
    /// </summary>
    public void InvalidateAvailability() =>
        this.checkAvailabilityAction.Schedule();


    /// <summary>
    /// Get name of dependency.
    /// </summary>
    public string Name
    {
        get
        {
            if (this.name == null)
                this.name = this.OnUpdateName();
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