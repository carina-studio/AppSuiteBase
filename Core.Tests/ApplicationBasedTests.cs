using CarinaStudio.Threading;
using NUnit.Framework;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite
{
	/// <summary>
	/// Base implementations of <see cref="IAppSuiteApplication"/> based tests.
	/// </summary>
	public abstract class ApplicationBasedTests
	{
		// Fields.
		volatile IAppSuiteApplication? app;


		/// <summary>
		/// Get <see cref="IAppSuiteApplication"/> instance.
		/// </summary>
		protected IAppSuiteApplication Application => this.app ?? throw new InvalidOperationException("Application is not ready.");


		/// <summary>
		/// Setup <see cref="IAppSuiteApplication"/> for testing.
		/// </summary>
		[OneTimeSetUp]
		public void SetupApplication()
		{
			MockAppSuiteApplication.Initialize();
			this.app = MockAppSuiteApplication.Current;
		}


		/// <summary>
		/// Get <see cref="SynchronizationContext"/> of application.
		/// </summary>
		protected SynchronizationContext SynchronizationContext => this.app?.SynchronizationContext ?? throw new InvalidOperationException("Application is not ready.");


		/// <summary>
		/// Run testing on thread of <see cref="IApplication"/>.
		/// </summary>
		/// <param name="test">Test action.</param>
		protected void TestOnApplicationThread(Action test)
		{
			var app = this.Application;
			if (app.CheckAccess())
				test();
			else
				app.SynchronizationContext.Send(test);
		}


		/// <summary>
		/// Run asynchronous testing on thread of <see cref="IAppSuiteApplication"/>.
		/// </summary>
		/// <param name="asyncTest">Asynchronous test action.</param>
		protected void TestOnApplicationThread(Func<Task> asyncTest)
		{
			var app = this.Application;
			if (app.CheckAccess())
				asyncTest();
			else
			{
				var syncLock = new object();
				var awaiter = new TaskAwaiter();
				lock (syncLock)
				{
					app.SynchronizationContext.Post(() =>
					{
						awaiter = asyncTest().GetAwaiter();
						awaiter.OnCompleted(() =>
						{
							lock (syncLock)
							{
								Monitor.Pulse(syncLock);
							}
						});
					});
					Monitor.Wait(syncLock);
					awaiter.GetResult();
				}
			}
		}
	}
}
