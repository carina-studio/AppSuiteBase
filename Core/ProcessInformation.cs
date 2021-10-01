using CarinaStudio.IO;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Provide information of current process.
    /// </summary>
    public class ProcessInformation : INotifyPropertyChanged
    {
        // Constants.
        const int ProcessInfoUpdateInterval = 1000;
		const int UIResponseCheckingInterval = 200;
		const int UIResponseUpdateInterval = 1000;


        // Fields.
        readonly AppSuiteApplication app;
		readonly ILogger logger;
		long previousProcessInfoUpdateTime;
		TimeSpan previousTotalProcessorTime;
		readonly Process process = Process.GetCurrentProcess();
        readonly SingleThreadSynchronizationContext processInfoCheckingSyncContext = new SingleThreadSynchronizationContext("Process information updater");
		readonly Stopwatch stopWatch = new Stopwatch().Also(it => it.Start());
		readonly Thread uiResponseCheckingThread;
        readonly ScheduledAction updateProcessInfoAction;


        // Constructor.
        internal ProcessInformation(AppSuiteApplication app)
        {
            // setup fields and properties
            this.app = app;
			this.logger = app.LoggerFactory.CreateLogger(nameof(ProcessInformation));
            this.ProcessId = this.process.Id;

            // create scheduled actions
            this.updateProcessInfoAction = new ScheduledAction(this.processInfoCheckingSyncContext, () =>
            {
				// get process info
				var privateMemoryUsage = 0L;
				var cpuUsagePercentage = double.NaN;
				var updateTime = this.stopWatch.ElapsedMilliseconds;
				try
				{
					this.process.Refresh();
					var totalProcessorTime = this.process.TotalProcessorTime;
					privateMemoryUsage = this.process.PrivateMemorySize64;
					if (this.previousProcessInfoUpdateTime > 0)
					{
						var processorTime = (totalProcessorTime - this.previousTotalProcessorTime);
						var updateInterval = (updateTime - this.previousProcessInfoUpdateTime);
						cpuUsagePercentage = (processorTime.TotalMilliseconds * 100.0 / updateInterval / Environment.ProcessorCount);
					}
					this.previousTotalProcessorTime = totalProcessorTime;
				}
				catch (Exception ex)
				{
					this.logger.LogError(ex, "Unable to get process info");
				}
				finally
				{
					this.previousProcessInfoUpdateTime = updateTime;
				}

				// report state
				if (!double.IsNaN(cpuUsagePercentage))
				{
					this.logger.LogTrace($"CPU usage: {cpuUsagePercentage:0.0}%");
					this.CpuUsagePercentage = cpuUsagePercentage;
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuUsagePercentage)));
				}
				if (privateMemoryUsage > 0)
				{
					this.logger.LogTrace($"Private memory usage: {privateMemoryUsage.ToFileSizeString()}");
					this.PrivateMemoryUsage = privateMemoryUsage;
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrivateMemoryUsage)));
				}
				this.updateProcessInfoAction?.Schedule(ProcessInfoUpdateInterval);
			});

			// start checking
			this.uiResponseCheckingThread = new Thread(this.UIResponseCheckingThreadEntry).Also(it =>
			{
				it.IsBackground = true;
				it.Name = "UI response checking thread";
				it.Start();
			});
			this.updateProcessInfoAction.Schedule();
        }


		/// <summary>
		/// Get CPU usage in percentage.
		/// </summary>
		public double? CpuUsagePercentage { get; private set; }


		/// <summary>
		/// Get private memory usage in bytes.
		/// </summary>
		public long? PrivateMemoryUsage { get; private set; }


		/// <summary>
		/// Get ID of current process;
		/// </summary>
		public int ProcessId { get; }


        /// <summary>
        /// Raised when property changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;


		// Entry of UI response checking thread.
		void UIResponseCheckingThreadEntry()
		{
			var stopWatch = new Stopwatch().Also(it => it.Start());
			var checkingId = 1L;
			var totalDuration = 0L;
			var checkingCount = 0;
			var lastReportTime = 0L;
			var syncLock = new object();
			while (true)
			{
				// check response duration
				var pingTime = stopWatch.ElapsedMilliseconds;
				var duration = 0L;
				lock (syncLock)
				{
					var localCheckingId = checkingId;
					this.app.SynchronizationContext.Post(() =>
					{
						duration = stopWatch.ElapsedMilliseconds - pingTime;
						lock (syncLock)
						{
							if (localCheckingId == checkingId)
								Monitor.Pulse(syncLock);
						}
					});
					if (!Monitor.Wait(syncLock, UIResponseUpdateInterval))
					{
						this.logger.LogError("UI is not responding");
						totalDuration = 0;
						checkingCount = 0;
						++checkingId;
						continue;
					}
					totalDuration += duration;
					++checkingCount;
					++checkingId;
				}
				Thread.Sleep(UIResponseCheckingInterval);

				// report later
				if ((stopWatch.ElapsedMilliseconds - lastReportTime) < UIResponseUpdateInterval)
					continue;

				// report respone duration
				var responseDuration = (totalDuration / checkingCount);
				totalDuration = 0;
				checkingCount = 0;
				lastReportTime = stopWatch.ElapsedMilliseconds;
				this.logger.LogTrace($"UI response duration: {responseDuration} ms");
				this.app.SynchronizationContext.Post(() =>
				{
					this.UIResponseDuration = TimeSpan.FromMilliseconds(responseDuration);
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UIResponseDuration)));
				});
			}
		}


		/// <summary>
		/// Get current duration of UI response.
		/// </summary>
		public TimeSpan? UIResponseDuration { get; private set; }
	}
}
