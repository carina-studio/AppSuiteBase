﻿using System;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
		/// <summary>
		/// Chinese (Simplified).
		/// </summary>
		ZH_CN,
	}


	/// <summary>
	/// Extensions for <see cref="ApplicationCulture"/>.
	/// </summary>
	public static class ApplicationCultureExtensions
    {
		// Statc fields.
		static volatile CultureInfo? CachedMacOSSysCultureInfo;
		static readonly Regex IetfLangTagRegex = new Regex("^(?<Language>[^\\-]+)\\-((?<Script>[^\\-]+)\\-)?(?<Region>[^\\-]+)$");
		static readonly Regex MacOSSysLangRegex = new Regex("^[\\s]*\"(?<Language>[^\"]*)\"");


		/// <summary>
		/// Convert to <see cref="CultureInfo"/>.
		/// </summary>
		/// <param name="culture"><see cref="ApplicationCulture"/>.</param>
		/// <param name="invalidateSysCultureInfo">True to invalidate system culture info before convertion.</param>
		/// <returns><see cref="CultureInfo"/>.</returns>
		public static CultureInfo ToCultureInfo(this ApplicationCulture culture, bool invalidateSysCultureInfo = false)
        {
			if (culture == ApplicationCulture.System)
			{
				if (Platform.IsMacOS)
				{
					if (invalidateSysCultureInfo || CachedMacOSSysCultureInfo == null)
					{
						try
						{
							using var process = Process.Start(new ProcessStartInfo()
							{
								Arguments = "read -g AppleLanguages",
								CreateNoWindow = true,
								FileName = "defaults",
								RedirectStandardOutput = true,
								UseShellExecute = false,
							});
							if (process != null)
							{
								using var reader = process.StandardOutput;
								var line = reader.ReadLine();
								while (line != null)
								{
									var match = MacOSSysLangRegex.Match(line);
									if (match.Success)
									{
										match = IetfLangTagRegex.Match(match.Groups["Language"].Value);
										if (match.Success)
										{
											try
											{
												
												CachedMacOSSysCultureInfo = CultureInfo.GetCultureInfo($"{match.Groups["Language"].Value}-{match.Groups["Region"].Value}");
												break;
											}
											catch
											{ }
										}
									}
									line = reader.ReadLine();
								}
							}
						}
						catch
						{ }
					}
					return CachedMacOSSysCultureInfo ?? CultureInfo.InstalledUICulture;
				}
				return CultureInfo.InstalledUICulture;
			}
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
