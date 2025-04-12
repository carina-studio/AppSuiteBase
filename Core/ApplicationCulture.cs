using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CarinaStudio.AppSuite;

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
	// Static fields.
	static volatile CultureInfo? CachedMacOSSysCultureInfo;
	static readonly Regex IetfLangTagRegex = new("^(?<Language>[^\\-]+)\\-((?<Script>[^\\-]+)\\-)?(?<Region>[^\\-]+)$");
	static readonly Regex MacOSSysLangRegex = new("^[\\s]*\"(?<Language>[^\"]*)\"");


	/// <summary>
	/// Get corresponding variant of Chinese.
	/// </summary>
	/// <param name="culture"><see cref="ApplicationCulture"/>.</param>
	/// <param name="invalidateSysCultureInfo">True to invalidate system culture info before conversion.</param>
	/// <returns>Variant of Chinese.</returns>
	public static ChineseVariant GetChineseVariant(this ApplicationCulture culture, bool invalidateSysCultureInfo = false) => culture switch
	{
		ApplicationCulture.ZH_CN => ChineseVariant.Default,
		ApplicationCulture.ZH_TW => ChineseVariant.Taiwan,
		_ => ApplicationCulture.System.GetCultureInfo(invalidateSysCultureInfo).GetChineseVariant(),
	};


	/// <summary>
	/// Get corresponding <see cref="CultureInfo"/>.
	/// </summary>
	/// <param name="culture"><see cref="ApplicationCulture"/>.</param>
	/// <param name="invalidateSysCultureInfo">True to invalidate system culture info before conversion.</param>
	/// <returns><see cref="CultureInfo"/>.</returns>
	public static CultureInfo GetCultureInfo(this ApplicationCulture culture, bool invalidateSysCultureInfo = false)
	{
		if (culture == ApplicationCulture.System)
		{
			if (Platform.IsMacOS)
			{
				if (invalidateSysCultureInfo || CachedMacOSSysCultureInfo == null)
				{
					try
					{
						using var process = Process.Start(new ProcessStartInfo
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
										// ReSharper disable once EmptyGeneralCatchClause
										catch
										{ }
									}
								}
								line = reader.ReadLine();
							}
						}
					}
					// ReSharper disable once EmptyGeneralCatchClause
					catch
					{ }
					return CachedMacOSSysCultureInfo ?? CultureInfo.InstalledUICulture;
				}
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


	/// <summary>
	/// Get corresponding <see cref="CultureInfo"/> asynchronously.
	/// </summary>
	/// <param name="culture"><see cref="ApplicationCulture"/>.</param>
	/// <param name="invalidateSysCultureInfo">True to invalidate system culture info before conversion.</param>
	/// <returns><see cref="CultureInfo"/>.</returns>
	public static Task<CultureInfo> GetCultureInfoAsync(this ApplicationCulture culture, bool invalidateSysCultureInfo = false) => Task.Run(() => 
		GetCultureInfo(culture, invalidateSysCultureInfo), CancellationToken.None);
}