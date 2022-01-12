using System;
using System.IO;
using System.Text.Json;

namespace CarinaStudio.AppSuite
{
    /// <summary>
    /// Object which supports saving/restoring state to/from JSON data.
    /// </summary>
    public interface IStateSavable
    {
        /// <summary>
        /// Check whether <see cref="RestoreState(JsonElement)"/> can be called in current state or not.
        /// </summary>
        bool CanRestoreState { get; }


        /// <summary>
        /// Restore state from JSON data.
        /// </summary>
        /// <param name="savedState">JSON element represents saved state.</param>
        /// <returns>True if state restored successfully.</returns>
        bool RestoreState(JsonElement savedState);


        /// <summary>
        /// Save state to JSON data.
        /// </summary>
        /// <param name="writer"><see cref="Utf8JsonWriter"/> to write saved state in JSON format.</param>
        /// <returns>True if state saved successfully.</returns>
        bool SaveState(Utf8JsonWriter writer);
    }


    /// <summary>
    /// Extensions for <see cref="IStateSavable"/>.
    /// </summary>
    public static class StateSavableExtensions
    {
        /// <summary>
        /// Ret restoring state.
        /// </summary>
        /// <param name="stateSavable"><see cref="IStateSavable"/>.</param>
        /// <param name="savedState">Saved state.</param>
        /// <returns>True if state restored successfully.</returns>
        public static bool TryRestoreState(this IStateSavable stateSavable, JsonElement savedState)
        {
            if (!stateSavable.CanRestoreState)
                return false;
            try
            {
                return stateSavable.RestoreState(savedState);
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Ret restoring state.
        /// </summary>
        /// <param name="stateSavable"><see cref="IStateSavable"/>.</param>
        /// <param name="savedState">Saved state.</param>
        /// <returns>True if state restored successfully.</returns>
        public static bool TryRestoreState(this IStateSavable stateSavable, byte[] savedState)
        {
            if (!stateSavable.CanRestoreState)
                return false;
            try
            {
                using var stream = new MemoryStream(savedState);
                using var document = JsonDocument.Parse(stream);
                return stateSavable.RestoreState(document.RootElement);
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Try saving state as <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="stateSavable"><see cref="IStateSavable"/>.</param>
        /// <param name="savedState"><see cref="JsonElement"/> represents saved state.</param>
        /// <returns>True if state saved successfully.</returns>
        public static bool TrySaveState(this IStateSavable stateSavable, out JsonElement savedState)
        {
            var element = new MemoryStream().Use(stream =>
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    if (!stateSavable.SaveState(writer))
                        return (JsonElement?)null;
                }
                stream.Position = 0;
                try
                {
                    return JsonDocument.Parse(stream).RootElement;
                }
                catch
                {
                    return null;
                }
            });
            savedState = element.GetValueOrDefault();
            return element.HasValue;
        }


        /// <summary>
        /// Try saving state as byte array.
        /// </summary>
        /// <param name="stateSavable"><see cref="IStateSavable"/>.</param>
        /// <param name="savedState">Saved state.</param>
        /// <returns>True if state saved successfully.</returns>
        public static bool TrySaveState(this IStateSavable stateSavable, out byte[]? savedState)
        {
            savedState = new MemoryStream().Use(stream =>
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    if (!stateSavable.SaveState(writer))
                        return null;
                }
                return stream.ToArray();
            });
            return (savedState != null);
        }
    }
}
