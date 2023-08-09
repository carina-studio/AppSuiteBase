using CarinaStudio.Configuration;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Configuration keys for simulations.
/// </summary>
public abstract class SimulationConfigurationKeys
{
    /// <summary>
    /// No network connection.
    /// </summary>
    public static readonly SettingKey<bool> NoNetworkConnection = new($"{NamePrefix}{nameof(NoNetworkConnection)}", false);
    
    
    // Constants.
    private const string NamePrefix = "Simulation.";
    
    
    // Constructor.
    SimulationConfigurationKeys()
    { }
}