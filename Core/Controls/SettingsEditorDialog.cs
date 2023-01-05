using CarinaStudio.Collections;
using CarinaStudio.Configuration;
using CarinaStudio.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// Dialog to edit values in given <see cref="ISettings"/> for debugging and tunning purpose.
    /// </summary>
    public class SettingsEditorDialog : CommonDialog<object?>
    {
        // Fields.
        readonly IEnumerable<SettingKey> readOnlySettingKeys;
        readonly List<SettingKey> settingKeys = new();
        ISettings? settings;


        /// <summary>
        /// Initialize new <see cref="SettingsEditorDialog"/> instance.
        /// </summary>
        public SettingsEditorDialog()
        {
            this.readOnlySettingKeys = this.settingKeys.AsReadOnly();
        }


        /// <summary>
        /// Get or set collection of key of setting to be shown in dialog.
        /// </summary>
        public IEnumerable<SettingKey> SettingKeys
        {
            get => this.readOnlySettingKeys;
            set
            {
                this.VerifyShowing();
                this.settingKeys.Clear();
                this.settingKeys.AddRange(value);
            }
        }


        /// <summary>
        /// Get or set <see cref="ISettings"/> which holds the values.
        /// </summary>
        public ISettings? Settings
        {
            get => this.settings;
            set
            {
                this.VerifyShowing();
                this.settings = value;
            }
        }


        /// <summary>
		/// Show dialog.
		/// </summary>
		/// <param name="owner">Owner window.</param>
		/// <returns>Task to showing dialog.</returns>
        public new Task ShowDialog(Avalonia.Controls.Window? owner) => base.ShowDialog(owner);


        /// <inheritdoc/>
        protected override Task<object?> ShowDialogCore(Avalonia.Controls.Window? owner)
        {
            if (this.settings == null)
                throw new InvalidOperationException("No Settings instance specified.");
            if (this.settingKeys.IsEmpty())
                return Task.FromResult((object?)null);
            var dialog = new SettingsEditorDialogImpl()
            {
                SettingKeys = new HashSet<SettingKey>(this.settingKeys),
                Settings = this.settings,
                Topmost = (owner?.Topmost).GetValueOrDefault(),
                WindowStartupLocation = owner != null 
                    ? Avalonia.Controls.WindowStartupLocation.CenterOwner
                    : Avalonia.Controls.WindowStartupLocation.CenterScreen,
            };
            return owner != null 
                ? dialog.ShowDialog<object?>(owner)
                : dialog.ShowDialog<object?>();
        }
    }
}