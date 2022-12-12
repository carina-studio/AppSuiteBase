using CarinaStudio.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Source of document.
/// </summary>
public abstract class DocumentSource : BaseApplicationObject<IAppSuiteApplication>, INotifyPropertyChanged
{
    // Fields.
    ApplicationCulture culture = ApplicationCulture.System;


    /// <summary>
    /// Initialize new <see cref="DocumentSource"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    protected DocumentSource(IAppSuiteApplication app) : base(app)
    { }


    /// <summary>
    /// Get or set culture of document.
    /// </summary>
    /// <value></value>
    public ApplicationCulture Culture
    {
        get => this.culture;
        set
        {
            this.VerifyAccess();
            if (this.culture == value)
                return;
            this.culture = value;
            this.OnPropertyChanged(nameof(Culture));
        }
    }


    /// <summary>
    /// Raise <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">Name of property.</param>
    protected virtual void OnPropertyChanged(string propertyName) =>
        this.PropertyChanged?.Invoke(this, new(propertyName));


    /// <summary>
    /// Raised when property changed.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <summary>
    /// Get list of all supported cultures.
    /// </summary>
    public abstract IList<ApplicationCulture> SupportedCultures { get; }


    /// <summary>
    /// Get URI of document.
    /// </summary>
    public abstract Uri Uri { get; }
}