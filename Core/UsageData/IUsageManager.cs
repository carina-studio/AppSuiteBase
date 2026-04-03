using CarinaStudio.Threading;
using System;
using System.Collections.Generic;

namespace CarinaStudio.AppSuite.UsageData;

/// <summary>
/// Manager for collecting application usage data and telemetry.
/// Session lifecycle and global context properties are managed automatically
/// by monitoring the application state.
/// Implementations are expected to be thread-safe.
/// </summary>
[ThreadSafe]
public interface IUsageManager : IApplicationObject<IAppSuiteApplication>
{
    /// <summary>
    /// Whether usage data collection is enabled.
    /// Reflects the user's privacy preference; all Track* calls are
    /// no-ops when <c>false</c>.
    /// </summary>
    bool IsEnabled { get; }


    /// <summary>
    /// Track a named user-action or feature-usage event.
    /// </summary>
    /// <param name="eventName">Short, dot-separated name, e.g. <c>"Editor.OpenFile"</c>.</param>
    /// <param name="properties">Optional key/value pairs describing the event context.</param>
    /// <param name="metrics">Optional numeric measurements, e.g. <c>{ "FileSize", 1024 }</c>.</param>
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
    
    
    /// <summary>
    /// Track a handled or unhandled exception.
    /// </summary>
    /// <param name="exception">Exception to track.</param>
    /// <param name="severityLevel">Severity classification.</param>
    /// <param name="properties">Optional additional properties.</param>
    void TrackException(Exception exception, UsageSeverityLevel severityLevel = UsageSeverityLevel.Error, IDictionary<string, string>? properties = null);
    
    
    /// <summary>
    /// Track a single numeric measurement, e.g. startup time or memory usage.
    /// </summary>
    /// <param name="metricName">Name of the metric, e.g. <c>"StartupTime"</c>.</param>
    /// <param name="value">Measured value.</param>
    /// <param name="properties">Optional additional properties.</param>
    void TrackMetric(string metricName, double value, IDictionary<string, string>? properties = null);


    /// <summary>
    /// Track that the user navigated to a named screen or opened a dialog.
    /// </summary>
    /// <param name="screenName">Logical name, e.g. <c>"MainWindow"</c>, <c>"SettingsDialog"</c>.</param>
    /// <param name="duration">Optional time the user spent on the screen.</param>
    /// <param name="properties">Optional additional properties.</param>
    void TrackScreenView(string screenName, TimeSpan? duration = null, IDictionary<string, string>? properties = null);
}


/// <summary>
/// Extension methods for <see cref="IUsageManager"/>.
/// </summary>
public static class UsageManagerExtensions
{
    /// <summary>
    /// Track an event with a single property.
    /// </summary>
    /// <param name="manager"><see cref="IUsageManager"/>.</param>
    /// <param name="eventName">Short, dot-separated name, e.g. <c>"Editor.OpenFile"</c>.</param>
    /// <param name="propertyKey">Property key.</param>
    /// <param name="propertyValue">Property value.</param>
    [ThreadSafe]
    public static void TrackEvent(this IUsageManager manager, string eventName, string propertyKey, string propertyValue) =>
        manager.TrackEvent(eventName, new Dictionary<string, string> { [propertyKey] = propertyValue });


    /// <summary>
    /// Track that the user navigated to a named screen or opened a dialog.
    /// </summary>
    /// <param name="manager"><see cref="IUsageManager"/>.</param>
    /// <param name="screenName">Logical name, e.g. <c>"MainWindow"</c>, <c>"SettingsDialog"</c>.</param>
    [ThreadSafe]
    public static void TrackScreenView(this IUsageManager manager, string screenName) =>
        manager.TrackScreenView(screenName, null, null);
}

