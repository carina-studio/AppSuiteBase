using System;

namespace CarinaStudio.AppSuite
{
	/// <summary>
	/// Application update info.
	/// </summary>
	public class ApplicationUpdateInfo
	{
		/// <summary>
		/// Initialize <see cref="ApplicationUpdateInfo"/> instance.
		/// </summary>
		/// <param name="version">Version of updated application.</param>
		/// <param name="releasePageUri">Uri of page of release.</param>
		/// <param name="packageUri">URI of update package.</param>
		public ApplicationUpdateInfo(Version version, Uri? releasePageUri, Uri? packageUri)
		{
			this.PackageUri = packageUri;
			this.ReleasePageUri = releasePageUri;
			this.Version = version;
		}


		/// <summary>
		/// Check equaliy.
		/// </summary>
		/// <param name="obj">Another object to check.</param>
		/// <returns>True if two objects are equivlent.</returns>
		public override bool Equals(object? obj)
		{
			if (obj is not ApplicationUpdateInfo updateInfo)
				return false;
			return this.Version == updateInfo.Version
				&& this.ReleasePageUri == updateInfo.ReleasePageUri
				&& this.PackageUri == updateInfo.PackageUri;
		}


		/// <summary>
		/// Calculate hash-code.
		/// </summary>
		/// <returns>Hash-code.</returns>
		public override int GetHashCode() => this.Version.GetHashCode();


		/// <summary>
		/// Equality operator.
		/// </summary>
		public static bool operator ==(ApplicationUpdateInfo? x, ApplicationUpdateInfo? y) => x?.Equals(y) ?? y is null;


		/// <summary>
		/// Inequality operator.
		/// </summary>
		public static bool operator !=(ApplicationUpdateInfo? x, ApplicationUpdateInfo? y) => !(x?.Equals(y) ?? y is null);


		/// <summary>
		/// Get URI of update package.
		/// </summary>
		public Uri? PackageUri { get; }


		/// <summary>
		/// Get Uri of page of release.
		/// </summary>
		public Uri? ReleasePageUri { get; }


		/// <summary>
		/// Get version of updated application.
		/// </summary>
		public Version Version { get; }
	}
}
