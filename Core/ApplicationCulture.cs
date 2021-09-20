using System;
using System.Globalization;
using System.Text;

namespace CarinaStudio.AppSuite
{
	/// <summary>
	/// Application culture.
	/// </summary>
	public enum ApplicationCulture
	{
		/// <summary>
		/// System.
		/// </summary>
		System,
		/// <summary>
		/// English (US).
		/// </summary>
		EN_US,
		/// <summary>
		/// Chinese (Taiwan).
		/// </summary>
		ZH_TW,
	}


	/// <summary>
	/// Extensions for <see cref="ApplicationCulture"/>.
	/// </summary>
	public static class ApplicationCultureExtensions
    {
		/// <summary>
		/// Convert to <see cref="CultureInfo"/>.
		/// </summary>
		/// <param name="culture"><see cref="ApplicationCulture"/>.</param>
		/// <returns><see cref="CultureInfo"/>.</returns>
		public static CultureInfo ToCultureInfo(this ApplicationCulture culture)
        {
			if (culture == ApplicationCulture.System)
				return CultureInfo.CurrentCulture;
			var nameBuilder = new StringBuilder(culture.ToString());
			for (var i = 0; i < nameBuilder.Length; ++i)
			{
				if (nameBuilder[i] == '_')
				{
					nameBuilder[i] = '-';
					break;
				}
				nameBuilder[i] = char.ToLowerInvariant(nameBuilder[i]);
			}
			return CultureInfo.GetCultureInfo(nameBuilder.ToString());
        }
    }
}
