using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Text;

namespace CarinaStudio.AppSuite.Converters
{
	/// <summary>
	/// <see cref="IValueConverter"/> to convert ratio to percentage.
	/// </summary>
	public class RatioToPercentageConverter : IValueConverter
	{
		/// <summary>
		/// Default instance without decimal place.
		/// </summary>
		public static readonly IValueConverter Default = new RatioToPercentageConverter(0);


		// Fields.
		readonly string? stringFormat;


		/// <summary>
		/// Initialize new <see cref="RatioToPercentageConverter"/> instance.
		/// </summary>
		/// <param name="decimalPlaces">Decimal places.</param>
		public RatioToPercentageConverter(int decimalPlaces)
		{
			if (decimalPlaces < 0)
				throw new ArgumentOutOfRangeException(nameof(decimalPlaces));
			if (decimalPlaces > 0)
			{
				this.stringFormat = new StringBuilder("{0:0.").Also(it =>
				{
					for (var i = decimalPlaces; i > 0; --i)
						it.Append('0');
					it.Append("}%");
				}).ToString();
			}
		}


		/// <inheritdoc/>
		public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			var ratio = value.Let(it =>
			{
				if (it is IConvertible convertible)
				{
					try
					{
						return convertible.ToDouble(null);
					}
					catch
					{ }
				}
				return double.NaN;
			});
			if (!double.IsFinite(ratio))
				return null;
			return targetType.Let(_ =>
			{
				if (targetType == typeof(string) || targetType == typeof(object))
				{
					if (this.stringFormat != null)
						return (object)string.Format(this.stringFormat, ratio * 100);
					return $"{(int)(ratio * 100 + 0.5)}%";
				}
				if (targetType == typeof(double))
					return ratio * 100;
				if (targetType == typeof(float))
					return (float)(ratio * 100);
				if (targetType == typeof(int))
					return (int)(ratio * 100 + 0.5);
				if (targetType == typeof(long))
					return (long)(ratio * 100 + 0.5);
				if (targetType == typeof(decimal))
					return (decimal)(ratio * 100 + 0.5);
				return null;
			});
		}


		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string str && str.Length > 1 && str[^1] == '%')
				value = str[0..^1];
			var ratio = value.Let(it =>
			{
				if (it is IConvertible convertible)
				{
					try
					{
						return convertible.ToDouble(null);
					}
					catch
					{ }
				}
				return double.NaN;
			});
			if (!double.IsFinite(ratio))
				return null;
			return targetType.Let(_ =>
			{
				if (targetType == typeof(double))
					return ratio / 100;
				if (targetType == typeof(float))
					return (float)(ratio / 100);
				return (object?)null;
			});
		}
	}
}
