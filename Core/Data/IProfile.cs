using CarinaStudio.Threading.Tasks;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Data;

/// <summary>
/// Generic profile.
/// </summary>
public interface IProfile<TApp> : IApplicationObject<TApp>, IEquatable<IProfile<TApp>>, INotifyPropertyChanged where TApp : class, IAppSuiteApplication
{
    /// <summary>
    /// Get unique ID of profile.
    /// </summary>
    string Id { get; }


    /// <summary>
    /// Check whether the profile is built-in or not.
    /// </summary>
    bool IsBuiltIn { get; }


    /// <summary>
    /// Get <see cref="TaskFactory"/> used by profile to perform I/O related tasks.
    /// </summary>
    TaskFactory IOTaskFactory { get; }


    /// <summary>
    /// Get <see cref="IProfileManager{TApp, TProfile}"/> which manages this profile.
    /// </summary>
    IProfileManager<TApp, IProfile<TApp>>? Manager { get; }


    /// <summary>
    /// Get or set name of profile.
    /// </summary>
    string? Name { get; set; }


    /// <summary>
    /// Save profile asynchronously.
    /// </summary>
    /// <param name="stream">Stream to write data of profile.</param>
    /// <param name="includeId">True to save <see cref="Id"/> also.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of saving profile.</returns>
    Task SaveAsync(Stream stream, bool includeId, CancellationToken cancellationToken = default);
}


/// <summary>
/// Extension methods for <see cref="IProfile{TApp}"/>.
/// </summary>
public static class ProfileExtensions
{
    /// <summary>
    /// Default <see cref="TaskFactory"/> for I/O tasks of profile.
    /// </summary>
    public static readonly TaskFactory IOTaskFactory = new(new FixedThreadsTaskScheduler(1));


    /// <summary>
    /// Save profile asynchronously.
    /// </summary>
    /// <param name="profile">Profile.</param>
    /// <param name="fileName">Name of file to write data of profile.</param>
    /// <param name="includeId">True to save <see cref="IProfile{TApp}.Id"/> also.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of saving profile.</returns>
    public static async Task SaveAsync<TApp>(this IProfile<TApp> profile, string fileName, bool includeId, CancellationToken cancellationToken = default) where TApp : class, IAppSuiteApplication
    {
        // open file
        if (cancellationToken.IsCancellationRequested)
            throw new TaskCanceledException();
        var stream = await IOTaskFactory.StartNew(() =>
        {
            if (CarinaStudio.IO.File.TryOpenReadWrite(fileName, 5000, out var stream))
                return stream.AsNonNull();
            throw new IOException($"Unable to open file '{fileName}'.");
        }, cancellationToken);
        if (cancellationToken.IsCancellationRequested)
        {
            Global.RunWithoutErrorAsync(stream.Close);
            throw new TaskCanceledException();
        }

        // save
        try
        {
            await profile.SaveAsync(stream, includeId, cancellationToken);
        }
        finally
        {
            Global.RunWithoutErrorAsync(stream.Close);
        }
    }


    /// <summary>
    /// Throw <see cref="InvalidOperationException"/> if given profile is built-in.
    /// </summary>
    /// <param name="profile">Profile.</param>
    public static void VerifyBuiltIn<TApp>(this IProfile<TApp> profile) where TApp : class, IAppSuiteApplication
    {
        if (profile.IsBuiltIn)
            throw new InvalidOperationException();
    }
}