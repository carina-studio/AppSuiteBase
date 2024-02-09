using CarinaStudio.AppSuite.Controls;
using CarinaStudio.Collections;
using CarinaStudio.IO;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Provide information of current process.
/// </summary>
public class ProcessInfo : INotifyPropertyChanged
{
	// Native symbols.
	[DllImport("/usr/lib/libSystem.dylib")]
    static extern int mach_timebase_info(out mach_timebase_info_t info);


	// Token for high-frequency update.
	class HighFrequencyUpdateToken : IDisposable
	{
		readonly ProcessInfo processInfo;
		public HighFrequencyUpdateToken(ProcessInfo processInfo) =>
			this.processInfo = processInfo;
		public void Dispose() =>
			this.processInfo.OnHfTokenDisposed(this);
	}


	// Time-base information for macOS.
#pragma warning disable IDE1006
    // ReSharper disable IdentifierTypo
	[StructLayout(LayoutKind.Sequential)]
    struct mach_timebase_info_t
    {
        public readonly uint numer;
        public readonly uint denom;
    }
    // ReSharper restore IdentifierTypo
#pragma warning restore IDE1006


    // Constants.
    const long AbnormalUIResponseCounter = 3;
    const int FirstUpdateDelay = 10000;
	const int ProcessInfoUpdateInterval = 3000;
    const int ProcessInfoUpdateIntervalBG = 10000;
	const int ProcessInfoUpdateIntervalHF = 1500;
	const int ProcessInfoUpdateIntervalHFInDebugMode = 1000;
	const int UIResponseCheckingInterval = 3000;
	const int UIResponseCheckingIntervalBG = 10000;
	const int UIResponseCheckingIntervalHF = 1000;
	const int UIResponseTimeout = 3000;


    // Fields.
    readonly IAppSuiteApplication app;
	readonly List<HighFrequencyUpdateToken> hfUpdateTokens = new();
	volatile bool isFirstUpdate = true;
	long latestGCCount;
	readonly ILogger logger;
	mach_timebase_info_t macOSTimebaseInfo;
	long previousProcessInfoUpdateTime;
	TimeSpan previousTotalProcessorTime;
	readonly Process process = Process.GetCurrentProcess();
    readonly SingleThreadSynchronizationContext processInfoCheckingSyncContext = new("Process information updater");
	readonly Stopwatch stopWatch = new Stopwatch().Also(it => it.Start());
	readonly object uiResponseCheckingSyncLock = new();
    readonly ScheduledAction updateProcessInfoAction;


    // Constructor.
    internal ProcessInfo(IAppSuiteApplication app)
    {
        // setup fields and properties
        this.app = app;
		this.logger = app.LoggerFactory.CreateLogger(nameof(ProcessInfo));
		this.ProcessId = this.process.Id;
		this.ThreadCount = 1;

        // create scheduled actions
        this.updateProcessInfoAction = new ScheduledAction(this.processInfoCheckingSyncContext, () =>
        {
            this.isFirstUpdate = false;
			this.Update();
		});

		// attach to application
		app.PropertyChanged += this.OnApplicationPropertyChanged;

		// start checking
		new Thread(this.UIResponseCheckingThreadEntry).Also(it =>
		{
			it.IsBackground = true;
			it.Name = "UI response checking thread";
			it.Start();
		});
		this.updateProcessInfoAction.Schedule(this.SelectUpdateInterval());
    }


	/// <summary>
	/// Get CPU usage in percentage.
	/// </summary>
	public double? CpuUsagePercentage { get; private set; }


	/// <summary>
	/// Get number of pending objects to be finalized.
	/// </summary>
	public long FinalizationPendingCount { get; private set; }


	/// <summary>
	/// Get average number of GC performed per second.
	/// </summary>
	/// <remarks>The property is available only in debug mode.</remarks>
	public double? GCFrequency { get; private set; }


	// Called when property of application changed.
	void OnApplicationPropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(IAppSuiteApplication.IsBackgroundMode))
		{
			if (!this.app.IsBackgroundMode)
			{
				if (!this.isFirstUpdate)
					this.updateProcessInfoAction.Reschedule(this.SelectUpdateInterval());
				lock (this.uiResponseCheckingSyncLock)
				{
					Monitor.PulseAll(this.uiResponseCheckingSyncLock);
				}
			}
			else if (!this.isFirstUpdate)
			{
				this.updateProcessInfoAction.Reschedule();
				lock (this.uiResponseCheckingSyncLock)
				{
					Monitor.PulseAll(this.uiResponseCheckingSyncLock);
				}
			}
		}
		else if (e.PropertyName == nameof(IAppSuiteApplication.LatestActiveWindow))
		{
			var window = this.app.LatestActiveWindow;
			if (window is not null && window is not SplashWindowImpl && this.isFirstUpdate)
			{
				this.updateProcessInfoAction.Reschedule();
				lock (this.uiResponseCheckingSyncLock)
				{
					Monitor.PulseAll(this.uiResponseCheckingSyncLock);
				}
			}
		}
	}


	// Called after disposing high-frequency update token.
	void OnHfTokenDisposed(HighFrequencyUpdateToken token)
	{
		this.app.VerifyAccess();
		this.hfUpdateTokens.Remove(token);
		if (this.hfUpdateTokens.IsEmpty())
			this.updateProcessInfoAction.Reschedule(this.SelectUpdateInterval());
	}


	/// <summary>
	/// Size of managed heap in bytes.
	/// </summary>
	/// <remarks>The property is available only in debug mode.</remarks>
	public long? ManagedHeapSize { get; private set; }


	/// <summary>
	/// Memory usage on managed heap in bytes.
	/// </summary>
	/// <remarks>The property is available only in debug mode.</remarks>
	public long? ManagedHeapUsage { get; private set; }


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
		this.app.VerifyAccess();
		var isFirstToken = false;
		var token = this.hfUpdateTokens.Lock(it =>
		{
			var token = new HighFrequencyUpdateToken(this);
			it.Add(token);
			isFirstToken = it.Count == 1;
			return token;
		});
		if (isFirstToken && !this.isFirstUpdate)
			this.updateProcessInfoAction.Reschedule(this.SelectUpdateInterval());
		return token;
	}


	// Select proper interval for checking UI response.
	int SelectUIResponseCheckingInterval()
	{
		if (this.isFirstUpdate)
			return FirstUpdateDelay;
		if (this.app.IsBackgroundMode)
			return UIResponseCheckingIntervalBG;
		if (app.IsDebugMode)
			return UIResponseCheckingIntervalHF;
		return UIResponseCheckingInterval;
	}


	// Select proper interval to update process info.
	int SelectUpdateInterval()
	{
		if (this.hfUpdateTokens.IsNotEmpty())
		{
			if (app.IsDebugMode)
				return ProcessInfoUpdateIntervalHFInDebugMode;
			return ProcessInfoUpdateIntervalHF;
		}
		if (this.isFirstUpdate)
			return FirstUpdateDelay;
		if (app.IsBackgroundMode)
			return ProcessInfoUpdateIntervalBG;
		return ProcessInfoUpdateInterval;
	}


	/// <summary>
	/// Get number of threads.
	/// </summary>
	public int ThreadCount { get; private set; }


	// Entry of UI response checking thread.
	// ReSharper disable FunctionNeverReturns
	[DoesNotReturn]
	void UIResponseCheckingThreadEntry()
	{
		var checkingId = 1L;
		var totalDuration = 0L;
		var checkingCount = 0;
		var uiNotRespondingCount = 0;
		var isReportingAbnormalUIResponse = false;
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
				if (Monitor.Wait(syncLock, UIResponseTimeout))
				{
					if (uiNotRespondingCount > 0)
					{
						uiNotRespondingCount = 0;
						this.logger.LogWarning("UI starts responding");
					}
					totalDuration += duration;
					++checkingCount;
					++checkingId;
				}
				else
				{
					this.logger.LogWarning("UI is not responding");
					++uiNotRespondingCount;
					totalDuration = 0;
					checkingCount = 0;
					++checkingId;
					if (uiNotRespondingCount > AbnormalUIResponseCounter && !isReportingAbnormalUIResponse)
					{
						this.logger.LogWarning("Abnormal UI response detected");
						if (this.app is AppSuiteApplication asApp)
						{
							isReportingAbnormalUIResponse = true;
							asApp.SynchronizationContext.Post(() =>
							{
								isReportingAbnormalUIResponse = false;
								asApp.OnAbnormalUIResponse();
							});
						}
					}
					continue;
				}
			}
			lock (this.uiResponseCheckingSyncLock)
			{
				Monitor.Wait(this.uiResponseCheckingSyncLock, this.SelectUIResponseCheckingInterval());
			}

			// report response duration
			var responseDuration = (totalDuration / checkingCount);
			totalDuration = 0;
			checkingCount = 0;
			if (this.app.IsDebugMode)
				this.logger.LogTrace("UI response duration: {responseDuration} ms", responseDuration);
			this.app.SynchronizationContext.Post(() =>
			{
				if (!this.UIResponseDuration.HasValue || Math.Abs(this.UIResponseDuration.Value.TotalMilliseconds - responseDuration) >= 0.5)
				{
					this.UIResponseDuration = TimeSpan.FromMilliseconds(responseDuration);
					this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UIResponseDuration)));
				}
			});
		}
	}
	// ReSharper restore FunctionNeverReturns


	/// <summary>
	/// Get current duration of UI response.
	/// </summary>
	public TimeSpan? UIResponseDuration { get; private set; }


	// Update process info.
	void Update()
	{
		// get process info
		var gcFrequency = double.NaN;
		var privateMemoryUsage = 0L;
		var managedHeapSize = 0L;
		var managedHeapUsage = 0L;
		var finalizationPendingCount = 0L;
		var cpuUsagePercentage = double.NaN;
		var threadCount = this.ThreadCount;
		var updateTime = this.stopWatch.ElapsedMilliseconds;
		try
		{
			this.process.Refresh();
			threadCount = this.process.Threads.Count;
			var totalProcessorTime = this.process.TotalProcessorTime;
			var gcMemoryInfo = GC.GetGCMemoryInfo(GCKind.Any);
			privateMemoryUsage = this.process.PrivateMemorySize64;
			if (privateMemoryUsage <= 0)
			{
				privateMemoryUsage = this.process.WorkingSet64;
				privateMemoryUsage = Math.Max(privateMemoryUsage, gcMemoryInfo.TotalCommittedBytes);
			}
			if (this.app.IsDebugMode)
			{
				managedHeapSize = gcMemoryInfo.TotalCommittedBytes;
				managedHeapUsage = gcMemoryInfo.HeapSizeBytes;
				var gcCount = 0L;
				var latestGCCount = this.latestGCCount;
				for (var i = GC.MaxGeneration; i >= 0; --i)
					gcCount += GC.CollectionCount(i);
				this.latestGCCount = gcCount;
				gcCount -= latestGCCount;
				if (this.previousProcessInfoUpdateTime > 0)
				{
					if (gcCount > 0)
						gcFrequency = (gcCount * 1000.0 / (updateTime - this.previousProcessInfoUpdateTime));
					else
						gcFrequency = 0;
				}
			}
			finalizationPendingCount = gcMemoryInfo.FinalizationPendingCount;
			if (this.previousProcessInfoUpdateTime > 0)
			{
				var processorTime = (totalProcessorTime - this.previousTotalProcessorTime);
				var updateInterval = (updateTime - this.previousProcessInfoUpdateTime);
				/* [Workaround] Fix CPU time on macOS
				 * Please refer to 'Apply Timebase Information to Mach Absolute Time Values' section in https://developer.apple.com/documentation/apple-silicon/addressing-architectural-differences-in-your-macos-code
				 * (Issue still exists on .NET 7 RC2)
				 */
				if (Platform.IsMacOS)
				{
					ref var timebaseInfo = ref this.macOSTimebaseInfo;
#pragma warning disable CA1806
					if (timebaseInfo.denom == 0)
						mach_timebase_info(out timebaseInfo);
#pragma warning restore CA1806
					if (timebaseInfo.denom > 0)
						processorTime = processorTime * timebaseInfo.numer / timebaseInfo.denom;
				}
				cpuUsagePercentage = (processorTime.TotalMilliseconds * 100.0 / updateInterval);
				if (Platform.IsNotMacOS)
					cpuUsagePercentage /= Environment.ProcessorCount;
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
		this.app.SynchronizationContext.Post(() =>
		{
			if (!double.IsNaN(cpuUsagePercentage)
				&& Math.Abs(this.CpuUsagePercentage.GetValueOrDefault() - cpuUsagePercentage) >= 0.05)
			{
				this.CpuUsagePercentage = cpuUsagePercentage;
				this.PropertyChanged?.Invoke(this, new(nameof(CpuUsagePercentage)));
			}
			if (double.IsFinite(gcFrequency) 
				&& Math.Abs(this.GCFrequency.GetValueOrDefault() - gcFrequency) >= 0.1)
			{
				this.GCFrequency = gcFrequency;
				this.PropertyChanged?.Invoke(this, new(nameof(GCFrequency)));
			}
			if (managedHeapSize > 0
				&& this.ManagedHeapSize.GetValueOrDefault() != managedHeapSize)
			{
				this.ManagedHeapSize = managedHeapSize;
				this.PropertyChanged?.Invoke(this, new(nameof(ManagedHeapSize)));
			}
			if (managedHeapUsage > 0
				&& this.ManagedHeapUsage.GetValueOrDefault() != managedHeapUsage)
			{
				this.ManagedHeapUsage = managedHeapUsage;
				this.PropertyChanged?.Invoke(this, new(nameof(ManagedHeapUsage)));
			}
			if (privateMemoryUsage > 0
				&& this.PrivateMemoryUsage.GetValueOrDefault() != privateMemoryUsage)
			{
				this.PrivateMemoryUsage = privateMemoryUsage;
				this.PropertyChanged?.Invoke(this, new(nameof(PrivateMemoryUsage)));
			}
			if (finalizationPendingCount != this.FinalizationPendingCount)
			{
				this.FinalizationPendingCount = finalizationPendingCount;
				this.PropertyChanged?.Invoke(this, new(nameof(FinalizationPendingCount)));
			}
			if (threadCount != this.ThreadCount)
			{
				this.ThreadCount = threadCount;
				this.PropertyChanged?.Invoke(this, new(nameof(ThreadCount)));
			}
		});
		if (this.app.IsDebugMode)
			this.logger.LogTrace("CPU usage: {cpuUsagePercentage}%, memory usage: {privateMemoryUsage}", $"{cpuUsagePercentage:0.0}", privateMemoryUsage.ToFileSizeString());
		this.updateProcessInfoAction.Schedule(this.SelectUpdateInterval());
	}
}
