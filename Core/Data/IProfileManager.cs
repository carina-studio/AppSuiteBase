using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Data;

/// <summary>
/// Manager of <see cref="IProfile{TApp}"/>.
/// </summary>
public interface IProfileManager<out TApp, out TProfile> : IApplicationObject<TApp> where TApp : class, IAppSuiteApplication where TProfile : IProfile<TApp>
{
    /// <summary>
    /// Get profile by ID, or Null if profile cannot be found.
    /// </summary>
    /// <param name="id">ID of profile.</param>
    /// <returns>Profile or Null if profile cannot be found.</returns>
    TProfile? GetProfileOrDefault(string id);


    /// <summary>
    /// Get all profiles managed by this instance.
    /// </summary>
    IReadOnlyList<TProfile> Profiles { get; }


    /// <summary>
    /// Wait for completion of all I/O tasks.
    /// </summary>
    /// <returns>Task of waiting for tasks.</returns>
    Task WaitForIOTaskCompletion();
}


/// <summary>
/// Extension methods for <see cref="IProfileManager{TApp, TProfile}"/>.
/// </summary>
public static class ProfileManagerExtensions
{
    // Fields.
    static readonly Random random = new();


    /// <summary>
    /// Generate random unique ID for profile which will be managed by given <see cref="IProfileManager{TApp, TProfile}"/>.
    /// </summary>
    /// <param name="manager"><see cref="IProfileManager{TApp, TProfile}"/>.</param>
    /// <returns>Generated ID.</returns>
    public static string GenerateProfileId<TApp, TProfile>(this IProfileManager<TApp, TProfile> manager) where TApp : class, IAppSuiteApplication where TProfile : IProfile<TApp>
    {
        var idBuffer = new char[8];
        while (true)
        {
            for (var i = idBuffer.Length - 1; i >= 0; --i)
            {
                var n = random.Next(36);
                idBuffer[i] = n <= 9 ? (char)('0' + n) : (char)('a' + (n - 10));
            }
            var id = new string(idBuffer);
            if (manager.GetProfileOrDefault(id) == null)
                return id;
        }
    }
}