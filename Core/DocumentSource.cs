using CarinaStudio.Collections;
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


/// <summary>
/// Extensions for <see cref="DocumentSource"/>.
/// </summary>
public static class DocumentSourceExtensions
{
    /// <summary>
    /// Set <see cref="DocumentSource.Culture"/> to current culture of application.
    /// </summary>
    /// <param name="source"><see cref="DocumentSource"/>.</param>
    /// <returns>True if <see cref="DocumentSource.Culture"/> has been set successfully.</returns>
    public static bool SetToCurrentCulture(this DocumentSource source)
    {
        // get current culture
        var cultureName = AppSuiteApplication.CurrentOrNull?.CultureInfo?.Name;
        if (string.IsNullOrEmpty(cultureName))
            return false;
        
        // select and set culture
        var cultures = source.SupportedCultures;
        if (cultures.IsEmpty())
            return false;
        if (cultureName.StartsWith("zh"))
        {
            if (cultureName.EndsWith("TW"))
            {
                if (cultures.Contains(ApplicationCulture.ZH_TW))
                {
                    source.Culture = ApplicationCulture.ZH_TW;
                    return (source.Culture == ApplicationCulture.ZH_TW);
                }
                if (cultures.Contains(ApplicationCulture.ZH_CN))
                {
                    source.Culture = ApplicationCulture.ZH_CN;
                    return (source.Culture == ApplicationCulture.ZH_CN);
                }
            }
            else
            {
                if (cultures.Contains(ApplicationCulture.ZH_CN))
                {
                    source.Culture = ApplicationCulture.ZH_CN;
                    return (source.Culture == ApplicationCulture.ZH_CN);
                }
                if (cultures.Contains(ApplicationCulture.ZH_TW))
                {
                    source.Culture = ApplicationCulture.ZH_TW;
                    return (source.Culture == ApplicationCulture.ZH_TW);
                }
            }
        }
        if (cultures.Contains(ApplicationCulture.EN_US))
        {
            source.Culture = ApplicationCulture.EN_US;
            return (source.Culture == ApplicationCulture.EN_US);
        }
        return false;
    }
}