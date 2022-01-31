using CarinaStudio.Collections;
using CarinaStudio.IO;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Provide information of current process.
    /// </summary>
    public class ProcessInfo : INotifyPropertyChanged
    {
		// Token for high-frequency update.
		class HighFrequencyUpdateToken : IDisposable
		{
			readonly ProcessInfo processInfo;
			public HighFrequencyUpdateToken(ProcessInfo processInfo) =>
				this.processInfo = processInfo;
			public void Dispose() =>
				this.processInfo.OnHfTokenDisposed(this);
		}


        // Constants.
        const int ProcessInfoUpdateInterval = 3000;
		const int ProcessInfoUpdateIntervalHF = 1000;
		const int UIResponseCheckingInterval = 500;
		const int UIResponseCheckingIntervalHF = 200;
		const int UIResponseUpdateInterval = 3000;
		const int UIResponseUpdateIntervalHF = 1000;


        // Fields.
        readonly IAppSuiteApplication app;
		readonly List<HighFrequencyUpdateToken> hfUpdateTokens = new List<HighFrequencyUpdateToken>();
		readonly ILogger logger;
		long previousProcessInfoUpdateTime;
		TimeSpan previousTotalProcessorTime;
		readonly Process process = Process.GetCurrentProcess();
        readonly SingleThreadSynchronizationContext processInfoCheckingSyncContext = new SingleThreadSynchronizationContext("Process information updater");
		readonly Stopwatch stopWatch = new Stopwatch().Also(it => it.Start());
		readonly int uiResponseCheckingInterval;
		readonly int uiResponseUpdateInterval;
		readonly Thread uiResponseCheckingThread;
		int updateInterval;
        readonly ScheduledAction updateProcessInfoAction;


        // Constructor.
        internal ProcessInfo(IAppSuiteApplication app)
        {
            // setup fields and properties
            this.app = app;
			this.logger = app.LoggerFactory.CreateLogger(nameof(ProcessInfo));
			this.uiResponseCheckingInterval = app.IsDebugMode ? UIResponseCheckingIntervalHF : UIResponseCheckingInterval;
			this.uiResponseUpdateInterval = app.IsDebugMode ? UIResponseUpdateIntervalHF : UIResponseUpdateInterval;
			this.updateInterval = app.IsDebugMode ? ProcessInfoUpdateIntervalHF : ProcessInfoUpdateInterval;
            this.ProcessId = this.process.Id;

            // create scheduled actions
            this.updateProcessInfoAction = new ScheduledAction(this.processInfoCheckingSyncContext, () =>
            {
				this.Update();
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


		// Called after disposing high-frequency update token.
		void OnHfTokenDisposed(HighFrequencyUpdateToken token)
		{
			lock (this.hfUpdateTokens)
			{
				if (!this.hfUpdateTokens.Remove(token))
					return;
				if (this.hfUpdateTokens.IsNotEmpty())
					return;
			}
			this.updateInterval = this.app.IsDebugMode ? ProcessInfoUpdateIntervalHF : ProcessInfoUpdateInterval;
		}


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


		/// <summary>
		/// Request updating in higher frequency.
		/// </summary>
		/// <returns>Token of request.</returns>
		public IDisposable RequestHighFrequencyUpdate()
		{
			var isFirstToken = false;
			var token = this.hfUpdateTokens.Lock(it =>
			{
				var token = new HighFrequencyUpdateToken(this);
				it.Add(token);
				isFirstToken = it.Count == 1;
				return token;
			});
			if (isFirstToken)
			{
				this.updateInterval = ProcessInfoUpdateIntervalHF;
				this.updateProcessInfoAction.Reschedule();
			}
			return token;
		}


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
					if (!Monitor.Wait(syncLock, this.uiResponseUpdateInterval))
					{
						this.logger.LogWarning("UI is not responding");
						totalDuration = 0;
						checkingCount = 0;
						++checkingId;
						continue;
					}
					totalDuration += duration;
					++checkingCount;
					++checkingId;
				}
				Thread.Sleep(this.uiResponseCheckingInterval);

				// report later
				if ((stopWatch.ElapsedMilliseconds - lastReportTime) < this.uiResponseUpdateInterval)
					continue;

				// report respone duration
				var responseDuration = (totalDuration / checkingCount);
				totalDuration = 0;
				checkingCount = 0;
				lastReportTime = stopWatch.ElapsedMilliseconds;
				if (this.app.IsDebugMode)
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


		// Update process info.
		void Update()
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
				if (privateMemoryUsage <= 0)
					privateMemoryUsage = this.process.WorkingSet64;
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
				this.CpuUsagePercentage = cpuUsagePercentage;
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuUsagePercentage)));
			}
			if (privateMemoryUsage > 0)
			{
				this.PrivateMemoryUsage = privateMemoryUsage;
				this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrivateMemoryUsage)));
			}
			if (this.app.IsDebugMode)
				this.logger.LogTrace($"CPU usage: {cpuUsagePercentage:0.0}%, memory usage: {privateMemoryUsage.ToFileSizeString()}");
			this.updateProcessInfoAction?.Schedule(this.updateInterval);
		}


		// Update process info by "top".
		void UpdateByTop()
		{
			// start process
			using var topProcess = Process.Start(new ProcessStartInfo()
			{
				Arguments = $"-l 10 -pid {Process.GetCurrentProcess().Id} -stats cpu -s {Math.Max(1, this.updateInterval / 1000)}",
				CreateNoWindow = true,
				FileName = "top",
				RedirectStandardOutput = true,
				UseShellExecute = false,
			});
			if (topProcess == null)
			{
				this.Update();
				return;
			}

			// update process info
			try
			{
				var processInfoRegex = new Regex("^(?<CpuUsage>[\\d]+(\\.[\\d]+)?)[\\s]*$");
				using var reader = topProcess.StandardOutput;
				var line = reader.ReadLine();
				while(line != null)
				{
					var match = processInfoRegex.Match(line);
					if (match.Success)
					{
						// report CPU usage
						var cpuUsage = double.Parse(match.Groups["CpuUsage"].Value) / Environment.ProcessorCount;
						if (cpuUsage == 0)
						{
							line = reader.ReadLine();
							continue;
						}
						this.logger.LogTrace($"CPU usage: {cpuUsage:0.0}%");
						this.CpuUsagePercentage = cpuUsage;
						this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CpuUsagePercentage)));

						// report memory usage
						this.process.Refresh();
						var privateMemoryUsage = this.process.PrivateMemorySize64;
						if (privateMemoryUsage <= 0)
							privateMemoryUsage = this.process.WorkingSet64;
						this.logger.LogTrace($"Private memory usage: {privateMemoryUsage.ToFileSizeString()}");
						this.PrivateMemoryUsage = privateMemoryUsage;
						this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PrivateMemoryUsage)));
					}
					line = reader.ReadLine();
				}
			}
			catch (Exception ex)
			{
				this.logger.LogError(ex, "Error occurred while getting process info by 'top'");
			}
			finally
			{
				Global.RunWithoutError(topProcess.Kill);
				this.updateProcessInfoAction.Reschedule(this.updateInterval);
			}
		}
	}
}
