using CarinaStudio.Collections;
using CarinaStudio.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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
    bool isInitialized;
    Task? initializationTask;
    readonly Dictionary<string, TProfile> profilesById = new();
    readonly SortedObservableList<TProfile> profiles;
    int profilesSavingCounter;
    readonly HashSet<TProfile> profilesToSave = new();
    EventHandler<IProfileManager<TApp, TProfile>, TProfile>? removingProfileHandlers;
    readonly ScheduledAction saveProfilesAction;


    /// <summary>
    /// Initialize new <see cref="BaseProfileManager{TApp, TProfile}"/> instance.
    /// </summary>
    /// <param name="app">Application.</param>
    protected BaseProfileManager(TApp app) : base(app)
    {
        this.Logger = app.LoggerFactory.CreateLogger(this.GetType().Name);
        this.profiles = new(this.CompareProfiles);
        this.Profiles = (IReadOnlyList<TProfile>)ListExtensions.AsReadOnly(this.profiles);
        this.saveProfilesAction = new(() => _ = this.SaveProfiles());
        this.SynchronizationContext.Post(this.Initialize);
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
        if (profile.Manager is not null)
        {
            if (ReferenceEquals(profile.Manager, this))
                throw new InvalidOperationException("The profile is already added to this manager.");
            throw new InvalidOperationException("The profile is already added to another manager.");
        }
        this.Logger.LogTrace("Add profile '{profileName}' ({profileId})", profile.Name, profile.Id);
        this.profilesById[profile.Id] = profile;
        this.profiles.Add(profile);
        profile.Manager = this;
        this.OnAttachToProfile(profile);

        // save profile
        if (saveProfile)
            this.ScheduleSavingProfile(profile);
    }


    /// <summary>
    /// Cancel all scheduled profiles to be saved.
    /// </summary>
    protected void CancelSavingProfiles()
    {
        this.VerifyAccess();
        this.profilesToSave.Clear();
        this.saveProfilesAction.Cancel();
    }


    /// <summary>
    /// Compare profiles for sorting profiles.
    /// </summary>
    /// <param name="lhs">Profile at left hand side.</param>
    /// <param name="rhs">Profile at right hand side.</param>
    /// <returns>Comparison result.</returns>
    protected virtual int CompareProfiles(TProfile lhs, TProfile rhs)
    {
        var result = string.Compare(lhs.Name, rhs.Name, true, CultureInfo.InvariantCulture);
        if (result != 0)
            return result;
        return string.CompareOrdinal(lhs.Id, rhs.Id);
    }


    /// <summary>
    /// Get profile by ID, or Null if profile cannot be found.
    /// </summary>
    /// <param name="id">ID of profile.</param>
    /// <returns>Profile or Null if profile cannot be found.</returns>
    protected TProfile? GetProfileOrDefault(string id)
    {
        this.profilesById.TryGetValue(id, out var profile);
        return profile;
    }


    // Initialize.
    void Initialize()
    {
        if (this.isInitialized)
            return;
        this.isInitialized = true;
        this.initializationTask = this.OnInitializeAsync();
    }


    /// <summary>
    /// Load profiles from files asynchronously.
    /// </summary>
    /// <returns>Task of loading profiles.</returns>
    protected async Task LoadProfilesAsync()
    {
        // get profile files
        this.VerifyAccess();
        if (this.Application.IsCleanMode)
        {
            this.Logger.LogWarning("Skip loading profiles in clean mode");
            return;
        }
        var profileFileNames = await this.OnGetProfileFilesAsync();
        this.Logger.LogDebug("{count} profile file(s) found", profileFileNames.Count);

        // load profiles
        var profileCount = 0;
        foreach (var fileName in profileFileNames)
        {
            try
            {
                var fileInfo = new FileInfo(fileName);
                if (!fileInfo.Exists)
                    continue;
                if (fileInfo.Length == 0)
                {
                    this.Logger.LogWarning("Delete empty file '{fileName}'.", fileName);
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (Exception ex)
                    {
                        this.Logger.LogError(ex, "Failed to delete empty file '{fileName}'.", fileName);
                    }
                    continue;
                }
                this.Logger.LogTrace("Load profile from '{fileName}'", fileName);
                var profile = await this.OnLoadProfileAsync(fileName);
                if (this.profilesById.ContainsKey(profile.Id))
                {
                    this.Logger.LogWarning("Skip duplicate profile '{profileName}' ({profileId})", profile.Name, profile.Id);
                    continue;
                }
                this.Logger.LogTrace("Add profile '{profileName}' ({profileId})", profile.Name, profile.Id);
                if (Path.GetFileNameWithoutExtension(fileName) == profile.Id)
                    this.AddProfile(profile, false);
                else
                {
                    this.Logger.LogWarning("Correct file name of profile '{profileName}' ({profileId})", profile.Name, profile.Id);
                    this.AddProfile(profile, true);
                    Global.RunWithoutErrorAsync(() => File.Delete(fileName));
                }
                ++profileCount;
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to load profile from '{fileName}'", fileName);
            }
        }
        this.Logger.LogDebug("{profileCount} profile(s) loaded", profileCount);
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


    /// <summary>
    /// Called to get list of name of profile files asynchronously.
    /// </summary>
    /// <returns>Task of getting list of files.</returns>
    protected virtual Task<IList<string>> OnGetProfileFilesAsync() => ProfileExtensions.IOTaskFactory.StartNew(() =>
    {
        try
        {
            if (Directory.Exists(this.ProfilesDirectory))
                return Directory.GetFiles(this.ProfilesDirectory, "*.json");
            return (IList<string>)Array.Empty<string>();
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Failed to get files in '{directory}'", this.ProfilesDirectory);
            return Array.Empty<string>();
        }
    });


    /// <summary>
    /// Called to initialize the manager instance asynchronously.
    /// </summary>
    /// <returns>Task of initialization.</returns>
    protected virtual async Task OnInitializeAsync()
    {
        this.Logger.LogTrace("Start initialization (default)");

        // attach to product manager
        this.Application.ProductManager.ProductActivationChanged += (_, productId, isActivated) =>
            this.OnProductActivationChanged(productId, isActivated);

        // load built-in profiles
        var builtInProfiles = await this.OnLoadBuiltInProfilesAsync();
        if (builtInProfiles.IsNotEmpty())
        {
            foreach (var profile in builtInProfiles)
            {
                this.Logger.LogTrace("Add built-in profile '{profileName}' ({profileId})", profile.Name, profile.Id);
                this.AddProfile(profile, false);
            }
            this.Logger.LogDebug("{count} built-in profile(s) loaded", builtInProfiles.Count);
        }

        // load profiles from files
        await this.LoadProfilesAsync();

        this.Logger.LogTrace("Complete initialization (default)");
    }


    /// <summary>
    /// Called to load built-in profiles asynchronously
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of loading built-in profiles.</returns>
    protected virtual Task<ICollection<TProfile>> OnLoadBuiltInProfilesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ICollection<TProfile>>(Array.Empty<TProfile>());
    

    /// <summary>
    /// Called to load profile from file asynchronously.
    /// </summary>
    /// <param name="fileName">Name of file to load profile from.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task of loading profile.</returns>
    protected abstract Task<TProfile> OnLoadProfileAsync(string fileName, CancellationToken cancellationToken = default);


    /// <summary>
    /// Called when activation state of product has been changed.
    /// </summary>
    /// <param name="productId">ID of product.</param>
    /// <param name="isActivated">Whether product is activated or not.</param>
    protected virtual void OnProductActivationChanged(string productId, bool isActivated)
    { }


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


    /// <summary>
    /// Raise <see cref="PropertyChanged"/> event.
    /// </summary>
    /// <param name="propertyName">Property name.</param>
    protected void OnPropertyChanged(string propertyName) =>
        this.PropertyChanged?.Invoke(this, new(propertyName));


    /// <summary>
    /// Called to save profile to file asynchronously.
    /// </summary>
    /// <param name="profile">Profile to be saved.</param>
    /// <param name="fileName">File name.</param>
    /// <returns>Task of saving profile.</returns>
    protected virtual Task OnSaveProfileAsync(TProfile profile, string fileName) =>
        profile.SaveAsync(fileName, true);


    /// <summary>
    /// Get all profiles managed by this instance.
    /// </summary>
    protected IReadOnlyList<TProfile> Profiles { get; }


    /// <summary>
    /// Get base directory for loading and saving profiles.
    /// </summary>
    protected abstract string ProfilesDirectory { get; }


    /// <summary>
    /// Remove given profile from this manager.
    /// </summary>
    /// <param name="profile">Profile to remove.</param>
    /// <param name="deleteFile">True to delete file of profile.</param>
    /// <returns>True if profile has been removed successfully.</returns>
    protected bool RemoveProfile(TProfile profile, bool deleteFile = true)
    {
        // check state
        this.VerifyAccess();
        
        // remove profile
        if (!ReferenceEquals(profile.Manager, this) || !this.profilesById.Remove(profile.Id))
            return false;
        this.Logger.LogTrace(deleteFile 
            ? "Remove profile '{profileName}' ({profileId})" 
            : "Remove profile '{profileName}' ({profileId}) without deleting file", profile.Name, profile.Id);
        this.removingProfileHandlers?.Invoke(this, profile);
        this.profiles.Remove(profile);
        this.profilesToSave.Remove(profile);
        this.OnDetachFromProfile(profile);
        profile.Manager = null;

        // delete file
        if (deleteFile)
        {
            var fileName = Path.Combine(this.ProfilesDirectory, $"{profile.Id}.json");
            profile.IOTaskFactory.StartNew(() =>
            {
                try
                {
                    if (File.Exists(fileName))
                    {
                        this.Logger.LogTrace("Delete file of profile '{profileName}' ({profileId})", profile.Name, profile.Id);
                        File.Delete(fileName);
                    }
                }
                catch (Exception ex)
                {
                    this.Logger.LogError(ex, "Failed to delete file of profile '{profileName}' ({profileId})", profile.Name, profile.Id);
                }
            });
        }

        // complete
        return true;
    }


    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;


    /// <summary>
    /// Raised before removing profile.
    /// </summary>
    protected event EventHandler<IProfileManager<TApp, TProfile>, TProfile>? RemovingProfile
    {
        add => this.removingProfileHandlers += value;
        remove => this.removingProfileHandlers -= value;
    }


    // Save profiles.
    async Task SaveProfiles()
    {
        if (this.profilesToSave.IsEmpty())
            return;
        ++this.profilesSavingCounter;
        this.Logger.LogTrace("Start saving profiles, counter: {counter}", this.profilesSavingCounter);
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
                this.Logger.LogTrace("Save profile '{profileId}'", profile.Id);
                await this.OnSaveProfileAsync(profile, Path.Combine(this.ProfilesDirectory, $"{profile.Id}.json"));
            }
            catch (Exception ex)
            {
                this.Logger.LogError(ex, "Failed to save profile '{profileId}'", profile.Id);
            }
        }
        --this.profilesSavingCounter;
        this.Logger.LogTrace("Complete saving profiles, counter: {counter}", this.profilesSavingCounter);
    }


    /// <summary>
    /// Schedule saving given profile to file.
    /// </summary>
    /// <param name="profile">Profile to save.</param>
    protected void ScheduleSavingProfile(TProfile profile)
    {
        this.VerifyAccess();
        if (!ReferenceEquals(profile.Manager, this))
            throw new ArgumentException("Profile is not managed by the manager.");
        if (this.Application.IsCleanMode)
        {
            this.Logger.LogWarning("Skip saving profile '{id}' in clean mode", profile.Id);
            return;
        }
        if (profile.IsBuiltIn || !this.profilesToSave.Add(profile))
            return;
        this.saveProfilesAction.Schedule(SavingProfilesDelay);
    }


    /// <summary>
    /// Schedule saving all profiles to files.
    /// </summary>
    protected void ScheduleSavingProfiles()
    {
        this.VerifyAccess();
        if (this.Application.IsCleanMode)
        {
            this.Logger.LogWarning("Skip saving profiles in clean mode");
            return;
        }
        foreach (var profile in this.profiles)
        {
            if (!profile.IsBuiltIn)
                this.profilesToSave.Add(profile);
        }
        if (this.profilesToSave.IsNotEmpty())
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


    /// <summary>
    /// Wait for completion of initialization.
    /// </summary>
    /// <returns>Task of waiting for completion.</returns>
    public async Task WaitForInitialization()
    {
        this.VerifyAccess();
        this.Initialize();
        if (this.initializationTask is not null)
        {
            await this.initializationTask;
            this.initializationTask = null;
        }
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
            this.Logger.LogTrace("Wait for profile saving counter: {counter}", this.profilesSavingCounter);
            await Task.Delay(200);
        }
        await this.profiles[0].IOTaskFactory.StartNew(() => this.Logger.LogInformation("I/O tasks completed"));
    }


    // Interface implementations.
    TProfile? IProfileManager<TApp, TProfile>.GetProfileOrDefault(string id) => this.GetProfileOrDefault(id);
    IReadOnlyList<TProfile> IProfileManager<TApp, TProfile>.Profiles => this.Profiles;
    event EventHandler<IProfileManager<TApp, TProfile>, TProfile>? IProfileManager<TApp, TProfile>.RemovingProfile
    {
        add => this.RemovingProfile += value;
        remove => this.RemovingProfile -= value;
    }
}