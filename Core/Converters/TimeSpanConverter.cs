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
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (targetType != typeof(string))
				return null;
			if (value is TimeSpan timeSpan)
			{
				if (this.app == null)
					return timeSpan.ToString();
				var isPositive = timeSpan.Ticks >= 0;
				var ms = Math.Abs(timeSpan.Milliseconds);
				var us = (long)((timeSpan.TotalMilliseconds - ((long)timeSpan.TotalMilliseconds)) * 1000);
				var secString = Math.Abs(timeSpan.Seconds).Let(sec =>
				{
					if (us != 0)
						return $"{sec}.{ms:D3}{Math.Abs(us):D3}";
					if (ms != 0)
						return $"{sec}.{ms:D3}";
					return sec.ToString();
				});
				if (timeSpan.Days != 0)
					return this.app.GetFormattedString("TimeSpanConverter.Days", timeSpan.Days, Math.Abs(timeSpan.Hours), Math.Abs(timeSpan.Minutes), secString);
				if (timeSpan.Hours != 0)
					return this.app.GetFormattedString("TimeSpanConverter.Hours", timeSpan.Hours, Math.Abs(timeSpan.Minutes), secString);
				if (timeSpan.Minutes != 0)
					return this.app.GetFormattedString("TimeSpanConverter.Minutes", timeSpan.Minutes, secString);
				if (timeSpan.Seconds != 0)
				{
					if (isPositive)
						return this.app.GetFormattedString("TimeSpanConverter.Seconds", secString);
					return this.app.GetFormattedString("TimeSpanConverter.Seconds", $"-{secString}");
				}
				if (ms != 0)
				{
					if (isPositive)
                    {
						if (us != 0)
							return this.app.GetFormattedString("TimeSpanConverter.Milliseconds", $"{ms}.{Math.Abs(us):D3}");
						return this.app.GetFormattedString("TimeSpanConverter.Milliseconds", ms);
					}
					else
                    {
						if (us != 0)
							return this.app.GetFormattedString("TimeSpanConverter.Milliseconds", $"-{ms}.{Math.Abs(us):D3}");
						return this.app.GetFormattedString("TimeSpanConverter.Milliseconds", -ms);
					}
				}
				else if (us != 0)
					return this.app.GetFormattedString("TimeSpanConverter.Microseconds", us);
				return this.app.GetFormattedString("TimeSpanConverter.Seconds", 0);
			}
			return null;
		}


		/// <summary>
		/// Convert string back to value.
		/// </summary>
		/// <remarks>The method is not implemented.</remarks>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => null;
	}
}
