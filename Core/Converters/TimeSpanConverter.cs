using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace CarinaStudio.AppSuite.Converters
{
	/// <summary>
	/// <see cref="IValueConverter"/> to convert <see cref="TimeSpan"/> to string.
	/// </summary>
	public class TimeSpanConverter : IValueConverter
	{
		/// <summary>
		/// Default instance.
		/// </summary>
		public static readonly TimeSpanConverter Default = new TimeSpanConverter(AppSuiteApplication.CurrentOrNull);


		// Fields.
		readonly IApplication? app;


		// Constructor.
		TimeSpanConverter(IApplication? app) => this.app = app;


		/// <summary>
		/// Convert value to string.
		/// </summary>
		/// <param name="value">Value to convert.</param>
		/// <param name="targetType">Target type which should be <see cref="string"/>.</param>
		/// <param name="parameter">Parameter which will be ignored.</param>
		/// <param name="culture"><see cref="CultureInfo"/>.</param>
		/// <returns>Converted string.</returns>
		public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (this.app == null)
				return null;
			if (targetType != typeof(string))
				return null;
			if (value is TimeSpan timeSpan)
			{
				if (timeSpan.Days > 0)
					return this.app.GetFormattedString("TimeSpanConverter.Days", timeSpan.Days, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
				if (timeSpan.Hours > 0)
					return this.app.GetFormattedString("TimeSpanConverter.Hours", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
				if (timeSpan.Minutes > 0)
					return this.app.GetFormattedString("TimeSpanConverter.Minutes", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
				if (timeSpan.Seconds > 0)
					return this.app.GetFormattedString("TimeSpanConverter.Seconds", timeSpan.Seconds, timeSpan.Milliseconds);
				return this.app.GetFormattedString("TimeSpanConverter.Milliseconds", timeSpan.Milliseconds);
			}
			return null;
		}


		/// <summary>
		/// Convert string back to value.
		/// </summary>
		/// <remarks>The method is not implemented.</remarks>
		public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => null;
	}
}
