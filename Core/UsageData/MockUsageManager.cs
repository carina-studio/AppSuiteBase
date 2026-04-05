using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.UsageData;

/// <summary>
/// No-op implementation of <see cref="IUsageManager"/>.
/// </summary>
internal class MockUsageManager(IAppSuiteApplication app) : BaseApplicationObject<IAppSuiteApplication>(app), IUsageManager
{
    /// <inheritdoc/>
    public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;


    /// <inheritdoc/>
    public bool IsEnabled => false;


#pragma warning disable CS0067
    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
#pragma warning restore CS0067


    /// <inheritdoc/>
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    { }


    /// <inheritdoc/>
    public void TrackException(Exception exception, UsageSeverityLevel severityLevel = UsageSeverityLevel.Error, IDictionary<string, string>? properties = null)
    { }


    /// <inheritdoc/>
    public void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null)
    { }


    /// <inheritdoc/>
    public void TrackScreenView(string screenName, TimeSpan? duration = null, IDictionary<string, string>? properties = null)
    { }
}
