using CarinaStudio.Collections;
using CarinaStudio.Threading;
using CarinaStudio.ViewModels;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.ViewModels
{
    /// <summary>
    /// View-model of UI to show change list of current application.
    /// </summary>
    public class ApplicationChangeList : ViewModel
    {
        // Fields.
        readonly ObservableList<ApplicationChange> changeList = new ObservableList<ApplicationChange>();
        readonly CancellationTokenSource changeListLoadingCancellationTokenSource = new CancellationTokenSource();


        /// <summary>
        /// Initialize new <see cref="ApplicationChangeList"/> instance.
        /// </summary>
        public ApplicationChangeList() : base(AppSuiteApplication.Current)
        {
            this.ChangeList = this.changeList.AsReadOnly();
            this.SynchronizationContext.Post(this.LoadChangeList);
        }


        /// <summary>
        /// Get list of <see cref="ApplicationChange"/>.
        /// </summary>
        public IList<ApplicationChange> ChangeList { get; }


        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            this.changeListLoadingCancellationTokenSource.Cancel();
            base.Dispose(disposing);
        }


        /// <summary>
        /// Check whether <see cref="ChangeList"/> is still under loading state or not.
        /// </summary>
        public bool IsLoadingChangedList { get; private set; } = true;


        // Load change list.
        async void LoadChangeList()
        {
            // check state
            if (this.IsDisposed)
                return;

            // load change list
            var changeList = (IEnumerable<ApplicationChange>)new ApplicationChange[0];
            try
            {
                changeList = await this.LoadChangeListAsync(this.changeListLoadingCancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                if (this.IsDisposed)
                    return;
                this.Logger.LogError(ex, "Failed to load change list");
            }

            // update change list
            if (this.IsDisposed)
                return;
            this.changeList.Clear();
            this.changeList.AddRange(changeList);
            this.IsLoadingChangedList = false;
            this.OnPropertyChanged(nameof(IsLoadingChangedList));
        }


        /// <summary>
        /// Load change list of application asynchronously.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task of loading change list.</returns>
        protected virtual Task<IEnumerable<ApplicationChange>> LoadChangeListAsync(CancellationToken cancellationToken)
        {
            // load from resource for current culture
            var culture = this.Application.CultureInfo;
            var assembly = this.Application.Assembly;
            var resourceNames = assembly.GetManifestResourceNames();
            if (culture.Name != "en-US")
            {
                var targetNamePosfix = $".ChangeList-{culture.Name}.json";
                foreach (var name in resourceNames)
                {
                    if (name.EndsWith(targetNamePosfix))
                    {
                        using var stream = assembly.GetManifestResourceStream(name).AsNonNull();
                        try
                        {
                            return Task.FromResult((IEnumerable<ApplicationChange>)ApplicationChange.LoadFromJson(stream));
                        }
                        catch
                        { }
                    }
                }
            }

            // load default resource
            foreach (var name in resourceNames)
            {
                if (name.EndsWith(".ChangeList.json"))
                {
                    using var stream = assembly.GetManifestResourceStream(name).AsNonNull();
                    try
                    {
                        return Task.FromResult((IEnumerable<ApplicationChange>)ApplicationChange.LoadFromJson(stream));
                    }
                    catch
                    { }
                }
            }

            // resource not found
            this.Logger.LogWarning("No embedded resource for change list found");
            return Task.FromResult((IEnumerable<ApplicationChange>)new ApplicationChange[0]);
        }


        /// <summary>
        /// Get application version.
        /// </summary>
        public Version Version { get => this.Application.Assembly.GetName().Version.AsNonNull(); }


        /// <summary>
        /// Wait for loading change list completed.
        /// </summary>
        /// <returns>Task of waiting.</returns>
        public Task WaitForChangeListReadyAsync() =>
            this.WaitForChangeListReadyAsync(new CancellationToken());


        /// <summary>
        /// Wait for loading change list completed.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task of waiting.</returns>
        public async Task WaitForChangeListReadyAsync(CancellationToken cancellationToken)
        {
            // check state
            this.VerifyAccess();
            if (this.IsDisposed || !this.IsLoadingChangedList)
                return;

            // wait for loading completed
            await Task.Run(async () =>
            {
                while (this.IsLoadingChangedList && !this.IsDisposed)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    await Task.Delay(100);
                }
            }, cancellationToken);
        }
    }


    /// <summary>
    /// Represent a change of current application.
    /// </summary>
    public class ApplicationChange
    {
        /// <summary>
        /// Initialize new <see cref="ApplicationChange"/> instance.
        /// </summary>
        /// <param name="type">Type of the change.</param>
        /// <param name="description">Description of the change.</param>
        /// <param name="detailsPageUri">URI of page for details of the change.</param>
        public ApplicationChange(ApplicationChangeType type, string description, Uri? detailsPageUri = null)
        {
            this.Description = description;
            this.DetailsPageUri = detailsPageUri;
            this.Type = type;
        }


        /// <summary>
        /// Get description of the change.
        /// </summary>
        public string Description { get; }


        /// <summary>
        /// Get URI of page for details of the change.
        /// </summary>
        public Uri? DetailsPageUri { get; }


        /// <summary>
        /// Check whether <see cref="DetailsPageUri"/> is valid or not.
        /// </summary>
        public bool HasDetailsPageUri { get => this.DetailsPageUri != null; }


        /// <summary>
        /// Load list of <see cref="ApplicationChange"/> from JSON data.
        /// </summary>
        /// <param name="stream">Stream to read JSON data from.</param>
        /// <returns>List of <see cref="ApplicationChange"/>.</returns>
        public static IList<ApplicationChange> LoadFromJson(Stream stream) =>
            JsonDocument.Parse(stream).Use(document =>
            {
                return LoadFromJson(document.RootElement);
            });


        /// <summary>
        /// Load list of <see cref="ApplicationChange"/> from JSON data.
        /// </summary>
        /// <param name="jsonElement">Root element of JSON data to load <see cref="ApplicationChange"/>.</param>
        /// <returns>List of <see cref="ApplicationChange"/>.</returns>
        public static IList<ApplicationChange> LoadFromJson(JsonElement jsonElement)
        {
            var changes = new List<ApplicationChange>();
            foreach (var jsonValue in jsonElement.EnumerateArray())
            {
                if (jsonValue.ValueKind == JsonValueKind.Object)
                {
                    if (!jsonValue.TryGetProperty(nameof(Description), out var descriptionProperty) || descriptionProperty.ValueKind != JsonValueKind.String)
                        continue;
                    var detailsPageUri = (Uri?)null;
                    var type = ApplicationChangeType.Unclassified;
                    if (jsonValue.TryGetProperty(nameof(DetailsPageUri), out var jsonProperty) && jsonProperty.ValueKind == JsonValueKind.String)
                        Uri.TryCreate(jsonProperty.GetString(), UriKind.Absolute, out detailsPageUri);
                    if (jsonValue.TryGetProperty(nameof(Type), out jsonProperty) && jsonProperty.ValueKind == JsonValueKind.String)
                        Enum.TryParse(jsonProperty.GetString(), out type);
                    changes.Add(new ApplicationChange(type, descriptionProperty.GetString().AsNonNull(), detailsPageUri));
                }
            }
            return changes.AsReadOnly();
        }


        /// <summary>
        /// Get type of the change.
        /// </summary>
        public ApplicationChangeType Type { get; }
    }


    /// <summary>
    /// Type of application change.
    /// </summary>
    public enum ApplicationChangeType
    {
        /// <summary>
        /// Unclassified.
        /// </summary>
        Unclassified,
        /// <summary>
        /// New feature.
        /// </summary>
        NewFeature,
        /// <summary>
        /// Improvement.
        /// </summary>
        Improvement,
        /// <summary>
        /// Existing behavior change.
        /// </summary>
        BehaviorChange,
        /// <summary>
        /// Bug fixing.
        /// </summary>
        BugFixing,
    }
}
