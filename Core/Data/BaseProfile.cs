using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Data;

/// <summary>
/// Base implementation of <see cref="IProfile{TApp}"/>.
/// </summary>
public abstract class BaseProfile<TApp> : BaseApplicationObject<TApp>, IProfile<TApp> where TApp : class, IAppSuiteApplication
{
    // Fields.
    string id;
    volatile ILogger? logger;
    string? name;


    /// <summary>
    /// Initialize new <see cref="BaseProfile{TApp}"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    /// <param name="id">Unique ID.</param>
    /// <param name="isBuiltIn">True if profile is built-in.</param>
    protected BaseProfile(TApp app, string id, bool isBuiltIn) : base(app)
    {
        this.id = id;
        this.IsBuiltIn = isBuiltIn;
    }


    /// <inheritdoc/>
    public abstract bool Equals(IProfile<TApp>? profile);


    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        obj is IProfile<TApp> profile && this.Equals(profile);


    /// <inheritdoc/>
    public override int GetHashCode() =>
        this.Id.GetHashCode();


    /// <inheritdoc/>
    public virtual string Id 
    { 
        get => this.id;
        protected set
        {
            this.VerifyAccess();
            this.VerifyBuiltIn();
            if (this.id == value)
                return;
            this.id = value;
            this.OnPropertyChanged(nameof(Id));
        }
    }


    /// <inheritdoc/>
    public bool IsBuiltIn { get; }


    /// <inheritdoc/>
    public TaskFactory IOTaskFactory { get => ProfileExtensions.IOTaskFactory; }


    /// <summary>
    /// Load profile from JSON format data.
    /// </summary>
    /// <param name="element">Root JSON element.</param>
    protected void Load(JsonElement element)
    {
        this.VerifyAccess();
        this.OnLoad(element);
    }


    /// <summary>
    /// Get logger.
    /// </summary>
    protected ILogger Logger
    {
        get
        {
            return this.logger ?? this.Lock(() =>
            {
                if (this.logger == null)
                    this.logger = this.Application.LoggerFactory.CreateLogger(this.GetType().Name);
                return logger;
            });
        }
    }


    /// <inheritdoc/>
    public IProfileManager<TApp, IProfile<TApp>>? Manager { get; internal set; }


    /// <inheritdoc/>
    public virtual string? Name
    {
        get => this.name;
        set
        {
            this.VerifyAccess();
            this.VerifyBuiltIn();
            if (this.name == value)
                return;
            this.name = value;
            this.OnPropertyChanged(nameof(Name));
        }
    }


    /// <summary>
    /// Called to load profile from JSON format data.
    /// </summary>
    /// <param name="element">Root JSON element.</param>
    protected abstract void OnLoad(JsonElement element);


    /// <summary>
    /// Raise <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">Property name.</param>
    protected void OnPropertyChanged(string propertyName) =>
        this.PropertyChanged?.Invoke(this, new(propertyName));
    

    /// <summary>
    /// Called to save profile in JSON format data.
    /// </summary>
    /// <param name="writer">JSON data writer.</param>
    /// <param name="includeId">True to save <see cref="Id"/> also.</param>
    protected abstract void OnSave(Utf8JsonWriter writer, bool includeId);


    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <inheritdoc/>
    public async Task SaveAsync(Stream stream, bool includeId, CancellationToken cancellationToken = default)
    {
        // save to memory
        var data = new MemoryStream().Use(memoryStream =>
        {
            using (var writer = new Utf8JsonWriter(memoryStream, new JsonWriterOptions() { Indented = true }))
                this.OnSave(writer, includeId);
            return memoryStream.ToArray();
        });

        // write to stream
        await this.IOTaskFactory.StartNew(() =>
            stream.Write(data));
    }


    /// <inheritdoc/>
    public override string ToString() =>
        this.IsBuiltIn ? $"{this.Name} ({this.Id}, Built-In)" : $"{this.Name} ({this.Id})";
}