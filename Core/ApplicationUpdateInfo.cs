using System;

namespace CarinaStudio.AppSuite;

/// <summary>
/// Application update info.
/// </summary>
/// <param name="version">Version of updated application.</param>
/// <param name="informationalVersion">Informational version of updated application.</param>
/// <param name="manifestUri">Uri of package manifest.</param>
/// <param name="releasePageUri">Uri of page of release.</param>
/// <param name="packageUri">URI of update package.</param>
public class ApplicationUpdateInfo(Version version, string? informationalVersion, Uri manifestUri, Uri? releasePageUri, Uri? packageUri) : IEquatable<ApplicationUpdateInfo>
{
	/// <inheritdoc/>
	public bool Equals(ApplicationUpdateInfo? other) =>
		other is not null
		&& this.Version == other.Version
		&& this.InformationalVersion == other.InformationalVersion
		&& this.PackageManifestUri == other.PackageManifestUri
		&& this.ReleasePageUri == other.ReleasePageUri
		&& this.PackageUri == other.PackageUri;


	/// <inheritdoc/>
	public override bool Equals(object? obj) =>
		obj is ApplicationUpdateInfo updateInfo
		&& Equals(updateInfo);


	/// <summary>
	/// Calculate hash-code.
	/// </summary>
	/// <returns>Hash-code.</returns>
	public override int GetHashCode() => this.Version.GetHashCode();


	/// <summary>
	/// Get informational version of updated application.
	/// </summary>
	public string? InformationalVersion { get; } = informationalVersion;


	/// <summary>
	/// Equality operator.
	/// </summary>
	public static bool operator ==(ApplicationUpdateInfo? x, ApplicationUpdateInfo? y) => x?.Equals(y) ?? y is null;


	/// <summary>
	/// Inequality operator.
	/// </summary>
	public static bool operator !=(ApplicationUpdateInfo? x, ApplicationUpdateInfo? y) => !(x?.Equals(y) ?? y is null);


	/// <summary>
	/// Get URI of package manifest.
	/// </summary>
	public Uri PackageManifestUri { get; } = manifestUri;


	/// <summary>
	/// Get URI of update package.
	/// </summary>
	public Uri? PackageUri { get; } = packageUri;


	/// <summary>
	/// Get Uri of page of release.
	/// </summary>
	public Uri? ReleasePageUri { get; } = releasePageUri;


	/// <summary>
	/// Get version of updated application.
	/// </summary>
	public Version Version { get; } = version;
}