using CarinaStudio.Collections;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Data;

/// <summary>
/// Base implementation of <see cref="IProfileManager{TApp, TProfile}"/>.
/// </summary>
public abstract class BaseProfileManager<TApp, TProfile> : BaseApplicationObject<TApp>, IProfileManager<TApp, TProfile> where TApp : class, IAppSuiteApplication where TProfile : BaseProfile<TApp>
{
    // Constants.
    const int SavingProfilesDelay = 500;


    // Fields.
    readonly Dictionary<string, TProfile> profilesById = new();
    readonly SortedObservableList<TProfile> profiles;
    int profilesSavingCounter;
    readonly HashSet<TProfile> profilesToSave = new();
    readonly ScheduledAction saveProfilesAction;


    /// <summary>
    /// Initialize new <see cref="BaseProfileManager{TApp, TProfile}"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    protected BaseProfileManager(TApp app) : base(app)
    {
        this.Logger = app.LoggerFactory.CreateLogger(this.GetType().Name);
        this.profiles = new(this.CompareProfiles);
        this.Profiles = (IReadOnlyList<TProfile>)this.profiles.AsReadOnly();
        this.saveProfilesAction = new(() => _ = this.SaveProfiles());
    }


    /// <summary>
    /// Add profile to this manager.
    /// </summary>
    /// <param name="profile">Profile.</param>
    /// <param name="saveProfile">True to save profile after adding.</param>
    protected void AddProfile(TProfile profile, bool saveProfile = true)
    {
        // check state
        this.VerifyAccess();
        if (profile.Manager != null)
        {
            if (object.ReferenceEquals(profile.Manager, this))
                throw new InvalidOperationException("The profile is already added to this manager.");
            throw new InvalidOperationException("The profile is already added to another manager.");
        }
        this.Logger.LogTrace($"Add profile '{profile.Id}' ({profile.Name})");
        this.profilesById[profile.Id] = profile;
        this.profiles.Add(profile);
        profile.Manager = this;
        this.OnAttachToProfile(profile);

        // save profile
        if (saveProfile)
            this.ScheduleSavingProfile(profile);
    }


    /// <summary>
    /// Compare profiles for sorting profiles.
    /// </summary>
    /// <param name="lhs">Profile at left hand side.</param>
    /// <param name="rhs">Profile at right hand side.</param>
    /// <returns>Comparison result.</returns>
    protected virtual int CompareProfiles(TProfile lhs, TProfile rhs)
    {
        var result = string.Compare(lhs.Name, rhs.Name);
        if (result != 0)
            return result;
        return lhs.Id.CompareTo(rhs.Id);
    }


    /// <inheritdoc/>
    public TProfile? GetProfileOrDefault(string id)
    {
        this.profilesById.TryGetValue(id, out var profile);
        return profile;
    }


    /// <summary>
    /// Get logger.
    /// </summary>
    protected ILogger Logger { get; }


    /// <summary>
    /// Called to attach to given profile.
    /// </summary>
    /// <param name="profile">Profile.</param>
    protected virtual void OnAttachToProfile(TProfile profile)
    {
        profile.PropertyChanged += this.OnProfilePropertyChanged;
    }


    /// <summary>
    /// Called to detach from given profile.
    /// </summary>
    /// <param name="profile">Profile.</param>
    protected virtual void OnDetachFromProfile(TProfile profile)
    {
        profile.PropertyChanged -= this.OnProfilePropertyChanged;
    }


    // Called when property of profile changed.
    void OnProfilePropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        this.OnProfilePropertyChanged((TProfile)sender!, e);


    /// <summary>
    /// Called when property of profile changed.
    /// </summary>
    /// <param name="profile">Profile.</param>
    /// <param name="e">Event data.</param>
    protected virtual void OnProfilePropertyChanged(TProfile profile, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IProfile<TApp>.Name))
            this.SortProfile(profile);
        this.ScheduleSavingProfile(profile);
    }


    /// <inheritdoc/>
    public IReadOnlyList<TProfile> Profiles { get; }


    /// <summary>
    /// Get base directory for loading and saving profiles.
    /// </summary>
    protected abstract string ProfilesDirectory { get; }


    /// <summary>
    /// Remove given profile from this manager.
    /// </summary>
    /// <param name="profile">Profile to remove.</param>
    /// <returns>True if profile has been removed successfully.</returns>
    protected bool RemoveProfile(TProfile profile)
    {
        // check state
        this.VerifyAccess();
        
        // remove profile
        if (!object.ReferenceEquals(profile.Manager, this) || !this.profilesById.Remove(profile.Id))
            return false;
        this.Logger.LogTrace($"Remove profile '{profile.Id}'");
        this.profiles.Remove(profile);
        this.profilesToSave.Remove(profile);
        this.OnDetachFromProfile(profile);
        profile.Manager = null;

        // delete file
        var fileName = Path.Combine(this.ProfilesDirectory, $"{profile.Id}.json");
        profile.IOTaskFactory.StartNew(() =>
        {
            try
            {
                if (File.Exists(fileName))
                {
                    this.Logger.LogTrace($"Delete file of profile '{profile.Id}'");
                    File.Delete(fileName);
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, $"Failed to delete file of profile '{profile.Id}'");
            }
        });

        // complete
        return true;
    }


    // Save profiles.
    async Task SaveProfiles()
    {
        if (this.profilesToSave.IsEmpty())
            return;
        ++this.profilesSavingCounter;
        this.Logger.LogTrace($"Start saving profiles, counter: {this.profilesSavingCounter}");
        var profiles = this.profilesToSave.ToArray().Also(_ =>
            this.profilesToSave.Clear());
        await ProfileExtensions.IOTaskFactory.StartNew(() =>
        {
            try
            {
                if (!Directory.Exists(this.ProfilesDirectory))
                {
                    this.Logger.LogWarning("Create profiles directory");
                    Directory.CreateDirectory(this.ProfilesDirectory);
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to create profiles directory");
            }
        });
        foreach (var profile in profiles)
        {
            try
            {
                this.Logger.LogTrace($"Save profile '{profile.Id}'");
                await profile.SaveAsync(Path.Combine(this.ProfilesDirectory, $"{profile.Id}.json"), true);
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, $"Failed to save profile '{profile.Id}'");
            }
        }
        --this.profilesSavingCounter;
        this.Logger.LogTrace($"Complete saving profiles, counter: {this.profilesSavingCounter}");
    }


    /// <summary>
    /// Schedule saving given profile to file.
    /// </summary>
    /// <param name="profile">Profile to save.</param>
    protected void ScheduleSavingProfile(TProfile profile)
    {
        this.VerifyAccess();
        if (!object.ReferenceEquals(profile.Manager, this))
            throw new ArgumentException();
        if (profile.IsBuiltIn || !this.profilesToSave.Add(profile))
            return;
        this.saveProfilesAction.Schedule(SavingProfilesDelay);
    }


    /// <summary>
    /// Move given profile to correct position in <see cref="Profiles"/> if needed.
    /// </summary>
    /// <param name="profile">Profile.</param>
    protected void SortProfile(TProfile profile)
    {
        this.VerifyAccess();
        this.profiles.Sort(profile);
    }


    /// <inheritdoc/>
    public virtual async Task WaitForIOTaskCompletion()
    {
        // check state
        this.VerifyAccess();

        // save profiles
        this.saveProfilesAction.Cancel();
        await this.SaveProfiles();

        // wait for I/O tasks
        if (this.profiles.IsEmpty())
            return;
        this.Logger.LogInformation("Start waiting for I/O tasks");
        while (this.profilesSavingCounter > 0)
        {
            this.Logger.LogTrace($"Wait for profile saving counter: {this.profilesSavingCounter}");
            await Task.Delay(200);
        }
        await this.profiles[0].IOTaskFactory.StartNew(() => this.Logger.LogInformation("I/O tasks completed"));
    }
}