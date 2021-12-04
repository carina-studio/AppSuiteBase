using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Styling;
using CarinaStudio.Threading;
using System;

namespace CarinaStudio.AppSuite.Controls
{
    /// <summary>
    /// <see cref="TextBox"/> which treat input text as given value with type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">Type of object.</typeparam>
    public abstract class ValueTextBox<T> : TextBox, IStyleable where T : struct
	{
		/// <summary>
		/// Property of <see cref="IsTextValid"/>.
		/// </summary>
		public static readonly AvaloniaProperty<bool> IsTextValidProperty = AvaloniaProperty.Register<ValueTextBox<T>, bool>(nameof(IsTextValid), true);
		/// <summary>
		/// Property of <see cref="ValidationDelay"/>.
		/// </summary>
		public static readonly AvaloniaProperty<int> ValidationDelayProperty = AvaloniaProperty.Register<ValueTextBox<T>, int>(nameof(ValidationDelay), 500, coerce: (_, it) => Math.Max(0, it));
		/// <summary>
		/// Property of <see cref="Value"/>.
		/// </summary>
		public static readonly AvaloniaProperty<T?> ValueProperty = AvaloniaProperty.Register<ValueTextBox<T>, T?>(nameof(Value), null);


		// Fields.
		readonly IObservable<object?> invalidTextBrush;
		IDisposable? invalidTextBrushBinding;
		readonly ScheduledAction validateAction;


		/// <summary>
		/// Initialize new <see cref="ValueTextBox{T}"/> instance.
		/// </summary>
		protected ValueTextBox()
		{
			this.invalidTextBrush = this.GetResourceObservable("Brush/TextBox.Foreground.Error");
			this.validateAction = new ScheduledAction(() => this.Validate());
		}


		/// <summary>
		/// Check equality of values.
		/// </summary>
		/// <param name="x">First value.</param>
		/// <param name="y">Second value.</param>
		/// <returns>True if two values are equalvant.</returns>
		protected virtual bool CheckValueEquality(T? x, T? y) => x?.Equals(y) ?? y == null;


		/// <summary>
		/// Convert value to text.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <returns>Converted text.</returns>
		protected virtual string? ConvertToText(T value) => value.ToString();


		/// <summary>
		/// Get whether input <see cref="TextBox.Text"/> represent a valid value or not.
		/// </summary>
		public bool IsTextValid { get => this.GetValue<bool>(IsTextValidProperty); }


		/// <inheritdoc/>
		protected override void OnPropertyChanged<TProperty>(AvaloniaPropertyChangedEventArgs<TProperty> change)
		{
			base.OnPropertyChanged(change);
			var property = change.Property;
			if (property == IsTextValidProperty)
			{
				if (this.IsTextValid)
					this.invalidTextBrushBinding = this.invalidTextBrushBinding.DisposeAndReturnNull();
				else
					this.invalidTextBrushBinding = this.Bind(ForegroundProperty, this.invalidTextBrush);
			}
			else if (property == TextProperty)
			{
				if (string.IsNullOrEmpty(this.Text))
					this.validateAction.Reschedule();
				else
					this.validateAction.Reschedule(this.ValidationDelay);
			}
			else if (property == ValidationDelayProperty)
			{
				if (this.validateAction.IsScheduled)
					this.validateAction.Reschedule(this.ValidationDelay);
			}
			else if (property == ValueProperty)
			{
				var value = (T?)(object?)change.NewValue.Value;
				if (value != null)
				{
					if (!this.Validate() || !this.CheckValueEquality(this.Value, value))
						this.Text = this.ConvertToText(value.Value);
				}
				else if (this.Text != null)
					this.Text = "";
			}
		}


		/// <inheritdoc/>
		protected override void OnTextInput(TextInputEventArgs e)
		{
			if (string.IsNullOrEmpty(this.Text))
			{
				var text = e.Text;
				if (text != null && text.Length > 0 && char.IsWhiteSpace(text[0]))
					e.Handled = true;
			}
			base.OnTextInput(e);
		}


		/// <summary>
		/// Try converting text to value.
		/// </summary>
		/// <param name="text">Text.</param>
		/// <param name="value">Converted value.</param>
		/// <returns>True if conversion succeeded.</returns>
		protected abstract bool TryConvertToValue(string text, out T? value);


		/// <summary>
		/// Validate input <see cref="TextBox.Text"/> and generate corresponding value.
		/// </summary>
		/// <returns>True if input <see cref="TextBox.Text"/> generates a valid value.</returns>
		public bool Validate()
		{
			// check state
			this.VerifyAccess();

			// cancel scheduled validation
			this.validateAction.Cancel();

			// trim spaces
			var text = this.Text ?? "";
			var trimmedText = text.Trim();
			if (text != trimmedText)
			{
				text = trimmedText;
				this.Text = trimmedText;
				this.validateAction.Cancel();
			}

			// clear object
			if (text.Length == 0)
			{
				this.SetValue<T?>(ValueProperty, null);
				this.SetValue<bool>(IsTextValidProperty, true);
				return true;
			}

			// try convert to object
			if (!this.TryConvertToValue(text, out var value) || value == null)
			{
				this.SetValue<bool>(IsTextValidProperty, false);
				return false;
			}

			// complete
			if (!this.CheckValueEquality(value, this.Value))
				this.SetValue<T?>(ValueProperty, value);
			this.SetValue<bool>(IsTextValidProperty, true);
			return true;
		}


		/// <summary>
		/// Get or set the delay of validating text after user typing in milliseconds.
		/// </summary>
		public int ValidationDelay
		{
			get => this.GetValue<int>(ValidationDelayProperty);
			set => this.SetValue<int>(ValidationDelayProperty, value);
		}


		/// <summary>
		/// Get or set value.
		/// </summary>
		public T? Value
		{
			get => this.GetValue<T?>(ValueProperty);
			set => this.SetValue<T?>(ValueProperty, value);
		}


		// Interface implementations.
		Type IStyleable.StyleKey => typeof(TextBox);
	}
}
