using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Management;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Hardware information.
    /// </summary>
    public class HardwareInfo : INotifyPropertyChanged
    {
        // Fields.
        readonly AppSuiteApplication app;
        readonly ScheduledAction checkGraphicsCardAction;
        readonly ManagementEventWatcher? graphicsCardWatcher;
        readonly ILogger logger;
        readonly SingleThreadSynchronizationContext hwCheckingSyncContext = new SingleThreadSynchronizationContext("Hardware info checker");


        // Constructor.
        internal HardwareInfo(AppSuiteApplication app)
        {
            // setup fields
            this.app = app;
            this.checkGraphicsCardAction = new ScheduledAction(this.hwCheckingSyncContext, this.CheckGraphicsCard);
            this.logger = app.LoggerFactory.CreateLogger(nameof(HardwareInfo));

            // start checking
            this.CheckGraphicsCard();

            // start monitoring hardware change
            if (Platform.IsWindows)
            {
#pragma warning disable CA1416
                this.graphicsCardWatcher = new ManagementEventWatcher("SELECT * FROM Win32_VideoController");
                this.graphicsCardWatcher.EventArrived += (_, e) => this.checkGraphicsCardAction.Schedule();
#pragma warning restore CA1416
            }
        }


        // Check graphics card.
        void CheckGraphicsCard()
        {
            var hasDedicatedGraphicsCard = (bool?)null;
            if (Platform.IsWindows)
            {
#pragma warning disable CA1416
                try
                {
                    using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                    foreach (var obj in searcher.Get())
                    {
                        obj["Description"]?.ToString()?.Let(it =>
                        {
                            it = it.ToLower();
                            if (it.StartsWith("nvidia") || it.Contains(" wddm "))
                            {
                                hasDedicatedGraphicsCard = true;
                            }
                        });
                        if (hasDedicatedGraphicsCard == true)
                            break;
                    }
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Failed to check graphics card");
                }
#pragma warning restore CA1416
            }
            this.app.SynchronizationContext.Post(() =>
            {
                if (this.HasDedicatedGraphicsCard != hasDedicatedGraphicsCard)
                {
                    this.logger.LogTrace($"Dedicated graphics card: {hasDedicatedGraphicsCard}");
                    this.HasDedicatedGraphicsCard = hasDedicatedGraphicsCard;
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDedicatedGraphicsCard)));
                }
            });
        }


        /// <summary>
        /// Check whether at least one dedicated graphics has been attached to device or not.
        /// </summary>
        public bool? HasDedicatedGraphicsCard { get; private set; }


        /// <summary>
        /// Raised when property changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
