using Avalonia;
using Avalonia.Controls;
using System;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Controls
{
	/// <summary>
	/// <see cref="TextBox"/> which accept regular expression.
	/// </summary>
	public class RegexTextBox : ObjectTextBox<Regex>
	{
		/// <summary>
		/// Property of <see cref="IgnoreCase"/>.
		/// </summary>
		public static readonly AvaloniaProperty<bool> IgnoreCaseProperty = AvaloniaProperty.Register<RegexTextBox, bool>(nameof(IgnoreCase), true);
		/// <summary>
		/// Property of <see cref="Regex"/>.
		/// </summary>
		public static readonly AvaloniaProperty<Regex?> RegexProperty = AvaloniaProperty.Register<RegexTextBox, Regex?>(nameof(Regex), coerce: (textBox, regex) =>
		{
			if (regex == null)
				return null;
			var ignoreCase = ((RegexTextBox)textBox).IgnoreCase;
			var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
			if (regex.Options != options)
				return new Regex(regex.ToString(), options);
			return regex;
		});


		/// <inheritdoc/>
		protected override bool CheckObjectEquality(Regex? x, Regex? y)
		{
			if (x == null)
				return y == null;
			if (y == null)
				return false;
			return x.ToString() == y.ToString() && x.Options == y.Options;
		}


        /// <summary>
        /// Get or set whether case in <see cref="Regex"/> can be ignored or not.
        /// </summary>
        public bool IgnoreCase
		{
			get => this.GetValue<bool>(IgnoreCaseProperty);
			set => this.SetValue<bool>(IgnoreCaseProperty, value);
		}


		/// <inheritdoc/>
		protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
		{
			base.OnPropertyChanged(change);
			var property = change.Property;
			if (property == IgnoreCaseProperty)
				this.Validate();
			else if (property == ObjectProperty)
			{
				var regex = change.NewValue.Value as Regex;
				if (!this.CheckObjectEquality(regex, this.Regex))
					this.SetValue<Regex?>(RegexProperty, regex);
			}
			else if (property == RegexProperty)
			{
				var regex = change.NewValue.Value as Regex;
				if (!this.CheckObjectEquality(regex, this.Object))
					this.SetValue<Regex?>(ObjectProperty, regex);
			}
		}


		/// <summary>
		/// Get or set <see cref="Regex"/>.
		/// </summary>
		public Regex? Regex
		{
			get => this.GetValue<Regex?>(RegexProperty);
			set => this.SetValue<Regex?>(RegexProperty, value);
		}


		/// <inheritdoc/>
        protected override bool TryConvertToObject(string text, out Regex? obj)
        {
            try
            {
				obj = new Regex(text, this.IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None);
				return true;
            }
			catch
            {
				obj = null;
				return false;
            }
        }
    }
}
