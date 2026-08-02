using CarinaStudio.Collections;
using CarinaStudio.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Source of document.
/// </summary>
/// <param name="app">Application.</param>
public abstract class DocumentSource(IAppSuiteApplication app) : BaseApplicationObject<IAppSuiteApplication>(app), INotifyPropertyChanged
{
    /// <summary>
    /// Get or set culture of document.
    /// </summary>
    public ApplicationCulture Culture
    {
        get;
        set
        {
            this.VerifyAccess();
            if (field == value)
                return;
            field = value;
            this.OnPropertyChanged(nameof(Culture));
        }
    } = ApplicationCulture.System;


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
        if (IAppSuiteApplication.CurrentOrNull?.CultureInfo is not { } cultureInfo)
            return false;
        
        // select and set culture
        var cultures = source.SupportedCultures;
        if (cultures.IsEmpty())
            return false;
        var targetCulture = ApplicationCulture.EN_US;
        if (cultureInfo.IsJapanese)
        {
            if (cultures.Contains(ApplicationCulture.JA_JP))
                targetCulture = ApplicationCulture.JA_JP;
        }
        else if (cultureInfo.IsChinese)
        {
            if (cultureInfo.ChineseVariant == ChineseVariant.Taiwan)
            {
                if (cultures.Contains(ApplicationCulture.ZH_TW))
                    targetCulture = ApplicationCulture.ZH_TW;
                else if (cultures.Contains(ApplicationCulture.ZH_CN))
                    targetCulture = ApplicationCulture.ZH_CN;
            }
            else
            {
                if (cultures.Contains(ApplicationCulture.ZH_CN))
                    targetCulture = ApplicationCulture.ZH_CN;
                else if (cultures.Contains(ApplicationCulture.ZH_TW))
                    targetCulture = ApplicationCulture.ZH_TW;
            }
        }
        if (cultures.Contains(targetCulture))
        {
            source.Culture = targetCulture;
            return source.Culture == targetCulture;
        }
        return false;
    }
}