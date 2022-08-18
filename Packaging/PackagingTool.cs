using CarinaStudio.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CarinaStudio.AppSuite.Packaging;

/// <summary>
/// Instance of packaging tool.
/// </summary>
public class PackagingTool
{
    // Constants.
    const string PackagesFolderName = "Packages";


    // Create diff packages.
    PackagingResult CreateDiffPackages(IList<string> args)
    {
        if (args.Count < 1)
        {
            Console.Error.Write("No previous version specified.");
            return PackagingResult.InvalidArgument;
        }
        if (args.Count < 2)
        {
            Console.Error.Write("No current version specified.");
            return PackagingResult.InvalidArgument;
        }
        var platform = args.Count > 2 ? args[0] : null;
        var prevVersionStr = args[platform != null ? 1 : 0];
        var currentVersionStr = args[platform != null ? 2 : 1];
        if (!Version.TryParse(prevVersionStr, out var prevVersion) || prevVersion == null)
        {
            Console.Error.Write($"Invalid previous version '{prevVersionStr}'.");
            return PackagingResult.InvalidArgument;
        }
        if (!Version.TryParse(currentVersionStr, out var currentVersion) || currentVersion == null)
        {
            Console.Error.Write($"Invalid current version '{currentVersionStr}'.");
            return PackagingResult.InvalidArgument;
        }
        if (prevVersion == currentVersion)
        {
            Console.Error.Write($"Previous and current version should be same.");
            return PackagingResult.InvalidArgument;
        }
        return this.CreateDiffPackages(platform, prevVersion, currentVersion);
    }
    PackagingResult CreateDiffPackages(string? platform, Version prevVersion, Version currentVersion)
    {
        var tempDir = Path.Combine(PackagesFolderName, "__Temp");
        try
        {
            // check directory
            var currentVersionDir = Path.Combine(PackagesFolderName, currentVersion.ToString());
            var prevVersionDir = Path.Combine(PackagesFolderName, prevVersion.ToString());
            if (!Directory.Exists(prevVersionDir))
            {
                Console.Error.Write($"Cannot find directory '{prevVersionDir}'");
                return PackagingResult.FileOrDirectoryNotFound;
            }
            if (!Directory.Exists(currentVersionDir))
            {
                Console.Error.Write($"Cannot find directory '{currentVersionDir}'");
                return PackagingResult.FileOrDirectoryNotFound;
            }

            // find all packages of current version
            var packageFileNameRegex = new Regex("^(?<AppName>[\\w\\d\\.]+)\\-(?<Version>[\\d\\.]+)\\-(?<PlatformId>[\\w]+\\-[\\w\\d]+)(?<FxDependent>\\-fx\\-dependent)?\\.zip$", RegexOptions.IgnoreCase);
            var currentVersionPackageNames = new HashSet<string>(CarinaStudio.IO.PathEqualityComparer.Default).Also(it =>
            {
                foreach (var path in Directory.EnumerateFiles(currentVersionDir, "*.zip"))
                {
                    var name = Path.GetFileName(path);
                    var match = packageFileNameRegex.Match(name);
                    if (match.Success && Version.Parse(match.Groups["Version"].Value) == currentVersion)
                    {
                        if (platform == null || match.Groups["PlatformId"].Value.StartsWith(platform))
                            it.Add(name);
                    }
                }
            });
            if (currentVersionPackageNames.IsEmpty())
                return PackagingResult.Success;
            
            // create packages
            var buffer1 = new byte[4096];
            var buffer2 = new byte[4096];
            foreach (var path in Directory.EnumerateFiles(prevVersionDir, "*.zip"))
            {
                // check file name and version
                var match = packageFileNameRegex.Match(Path.GetFileName(path));
                if (!match.Success || Version.Parse(match.Groups["Version"].Value) != prevVersion)
                    continue;
                if (platform != null && !match.Groups["PlatformId"].Value.StartsWith(platform))
                    continue;
                
                // find current version of package
                var isFxDependent = match.Groups["FxDependent"].Success;
                var currentVersionPackageName = isFxDependent 
                    ? $"{match.Groups["AppName"].Value}-{currentVersion}-{match.Groups["PlatformId"].Value}-fx-dependent.zip"
                    : $"{match.Groups["AppName"].Value}-{currentVersion}-{match.Groups["PlatformId"].Value}.zip";
                if (!currentVersionPackageNames.Contains(currentVersionPackageName))
                    continue;
                
                // outout information
                if (isFxDependent)
                    Console.WriteLine($"Checking diff package for {match.Groups["PlatformId"].Value} (Framework Dependent)");
                else
                    Console.WriteLine($"Checking diff package for {match.Groups["PlatformId"].Value}");
                
                // create temp directory
                var tempPrevVersionDir = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(path));
                var tempCurrentVersionDir = Path.Combine(tempDir, Path.GetFileNameWithoutExtension(currentVersionPackageName));
                Directory.CreateDirectory(tempPrevVersionDir);
                Directory.CreateDirectory(tempCurrentVersionDir);

                // extract packages
                ZipFile.ExtractToDirectory(path, tempPrevVersionDir);
                ZipFile.ExtractToDirectory(Path.Combine(currentVersionDir, currentVersionPackageName), tempCurrentVersionDir);

                // filter and keep diff files
                var tempCurrentVersionSubDirQueue = new Queue<string>().Also(it => it.Enqueue(tempCurrentVersionDir));
                while (tempCurrentVersionSubDirQueue.TryDequeue(out var tempCurrentVersionSubDir))
                {
                    foreach (var currentFile in new DirectoryInfo(tempCurrentVersionSubDir).GetFiles())
                    {
                        var prevFile = new FileInfo(Path.Combine(tempPrevVersionDir, Path.GetRelativePath(tempCurrentVersionSubDir, currentFile.FullName)));
                        if (!prevFile.Exists || prevFile.Length != currentFile.Length)
                            continue;
                        using var prevFileStream = new FileStream(prevFile.FullName, FileMode.Open, FileAccess.Read);
                        using var currentFileStream = new FileStream(currentFile.FullName, FileMode.Open, FileAccess.Read);
                        var areSameFiles = true;
                        var readCount = prevFileStream.Read(buffer1, 0, buffer1.Length);
                        while (readCount > 0)
                        {
                            if (currentFileStream.Read(buffer2, 0, buffer2.Length) != readCount)
                            {
                                areSameFiles = false;
                                break;
                            }
                            for (var i = readCount - 1; i >= 0; --i)
                            {
                                if (buffer1[i] != buffer2[i])
                                {
                                    areSameFiles = false;
                                    break;
                                }
                            }
                            if (!areSameFiles)
                                break;
                            readCount = prevFileStream.Read(buffer1, 0, buffer1.Length);
                        }
                        if (areSameFiles)
                        {
                            currentFileStream.Close();
                            currentFile.Delete();
                        }
                    }
                }

                // compress
                var diffPackageName = isFxDependent
                    ? $"{match.Groups["AppName"].Value}-{prevVersion}-{currentVersion}-{match.Groups["PlatformId"].Value}-fx-dependent.zip"
                    : $"{match.Groups["AppName"].Value}-{prevVersion}-{currentVersion}-{match.Groups["PlatformId"].Value}.zip";
                var diffPackagePath = Path.Combine(currentVersionDir, diffPackageName);
                Console.WriteLine($"Generating diff package '{diffPackageName}'");
                File.Delete(diffPackagePath);
                ZipFile.CreateFromDirectory(tempCurrentVersionDir, diffPackagePath, CompressionLevel.SmallestSize, false);

                // delete temp directory
                Directory.Delete(tempPrevVersionDir, true);
                Directory.Delete(tempCurrentVersionDir, true);
            }
            return PackagingResult.Success;
        }
        catch (Exception ex)
        {
            Console.Error.Write($"Failed to create diff packages: {ex.Message}");
            return PackagingResult.UnclassifiedError;
        }
        finally
        {
            Global.RunWithoutError(() => Directory.Delete(tempDir, true));
        }
    }


    // Create packge manifest.
    PackagingResult CreatePackageManifest(IList<string> args)
    {
        if (args.Count < 1)
        {
            Console.Error.Write("No GitHub repositary specified.");
            return PackagingResult.InvalidArgument;
        }
        if (args.Count < 2)
        {
            Console.Error.Write("No version specified.");
            return PackagingResult.InvalidArgument;
        }
        var platform = args.Count > 2 ? args[0] : null;
        var repositaryName = args[platform != null ? 1 : 0];
        var versionStr = args[platform != null ? 2 : 1];
        if (!Version.TryParse(versionStr, out var version) || version == null)
        {
            Console.Error.Write($"Invalid version '{versionStr}'.");
            return PackagingResult.InvalidArgument;
        }
        return this.CreatePackageManifest(platform, repositaryName, version);
    }
    PackagingResult CreatePackageManifest(string? platform, string repositaryName, Version version)
    {
        try
        {
            // check directory
            var packageFileNameRegex = new Regex("^(?<AppName>[\\w\\d\\.]+)(\\-(?<PrevVersion>[\\d\\.]+))?-(?<Version>[\\d\\.]+)\\-(?<PlatformId>[\\w]+\\-[\\w\\d]+)(?<FxDependent>\\-fx\\-dependent)?\\.zip$", RegexOptions.IgnoreCase);
            var packagesDir = Path.Combine(PackagesFolderName, version.ToString());
            if (!Directory.Exists(packagesDir))
            {
                Console.Error.Write($"Packages directory '{packagesDir}' not found.");
                return PackagingResult.FileOrDirectoryNotFound;
            }

            // collect and sort package files
            var appName = (string?)null;
            var packageFilPaths = new List<string>().Also(it =>
            {
                foreach (var path in Directory.EnumerateFiles(packagesDir))
                {
                    var name = Path.GetFileName(path);
                    var match = packageFileNameRegex.Match(name);
                    if (!match.Success || Version.Parse(match.Groups["Version"].Value) != version)
                        continue;
                    if (platform != null && !match.Groups["PlatformId"].Value.StartsWith(platform))
                        continue;
                    if (appName == null)
                        appName = match.Groups["AppName"].Value;
                    else if (appName != match.Groups["AppName"].Value)
                        continue;
                    it.Add(path);
                }
            });
            packageFilPaths.Sort((lhs, rhs) =>
            {
                var result = rhs.Length - lhs.Length;
                if (result != 0)
                    return result;
                return string.Compare(lhs, rhs, false, CultureInfo.InvariantCulture);
            });
            if (packageFilPaths.IsEmpty())
            {
                Console.WriteLine("No packages to generate package manifest.");
                return PackagingResult.Success;
            }

            // generate manifest
            var manifestName = platform != null ? $"PackageManifest-{platform}.json" : "PackageManifest.json";
            Console.WriteLine($"Creating package manifest '{manifestName}'");
            using var stream = new FileStream(Path.Combine(packagesDir, manifestName), FileMode.Create, FileAccess.ReadWrite);
            using var jsonWriter = new Utf8JsonWriter(stream, new JsonWriterOptions() { Indented = true });
            jsonWriter.WriteStartObject();
            jsonWriter.WriteString("Name", appName);
            jsonWriter.WriteString("Version", version.ToString());
            jsonWriter.WriteString("PageUri", $"https://github.com/carina-studio/{repositaryName}/releases/tag/{version}");
            jsonWriter.WritePropertyName("Packages");
            jsonWriter.WriteStartArray();
            foreach (var path in packageFilPaths)
            {
                // calculate SHA256 checksum
                var name = Path.GetFileName(path);
                var match = packageFileNameRegex.Match(name);
                Console.WriteLine($"Including '{name}' into package manifest");
                var sha256 = new FileStream(path, FileMode.Open, FileAccess.Read).Use(stream =>
                {
                    using var hashAlgorithm = System.Security.Cryptography.SHA256.Create();
                    var hashBytes = hashAlgorithm.ComputeHash(stream);
                    return new StringBuilder(hashBytes.Length * 2).Also(it =>
                    {
                        for (int i = 0, count = hashBytes.Length; i < count; ++i)
                            it.AppendFormat("{0:X2}", hashBytes[i]);
                    }).ToString();
                });

                // get architecture
                var architecture = match.Groups["PlatformId"].Value.Let(it =>
                {
                    if (it.EndsWith("-arm64"))
                        return nameof(Architecture.Arm64);
                    if (it.EndsWith("-x64"))
                        return nameof(Architecture.X64);
                    if (it.EndsWith("-x86"))
                        return nameof(Architecture.X86);
                    return null;
                });

                // get OS
                var os = match.Groups["PlatformId"].Value.Let(it =>
                {
                    if (it.StartsWith("win-"))
                        return nameof(OSPlatform.Windows);
                    if (it.StartsWith("osx-"))
                        return nameof(OSPlatform.OSX);
                    if (it.StartsWith("linux-"))
                        return nameof(OSPlatform.Linux);
                    return null;
                });

                // write entry
                var platformId = match.Groups["PlatformId"];
                jsonWriter.WriteStartObject();
                architecture?.Let(it => jsonWriter.WriteString("Architecture", it));
                if (match.Groups["PrevVersion"].Success)
                    jsonWriter.WriteString("BaseVersion", match.Groups["PrevVersion"].Value);
                os?.Let(it => jsonWriter.WriteString("OperatingSystem", it));
                if (match.Groups["FxDependent"].Success)
                {
                    jsonWriter.WriteString("FrameworkVersion", "99.99");
                    jsonWriter.WriteString("RuntimeVersion", "6.0.1");   
                }
                jsonWriter.WriteString("SHA256", sha256);
                jsonWriter.WriteString("Uri", $"https://github.com/carina-studio/{repositaryName}/releases/download/{version}/{name}");
                jsonWriter.WriteEndObject();
            }
            jsonWriter.WriteEndArray();
            jsonWriter.WriteEndObject();
            return PackagingResult.Success;
        }
        catch (Exception ex)
        {
            Console.Error.Write($"Failed to create package manifest: {ex.Message}");
            return PackagingResult.UnclassifiedError;
        }
    }


    // Get current version of given project.
    PackagingResult GetCurrentVersion(IList<string> args)
    {
        if (args.IsEmpty())
        {
            Console.Error.WriteLine("No project specified.");
            return PackagingResult.InvalidArgument;
        }
        var result = this.GetCurrentVersion(args[0], out var version);
        if (result == PackagingResult.Success)
            Console.Write(version);
        else
            Console.Error.Write($"Unable to get current version of '{args[0]}'");
        return result;
    }
    PackagingResult GetCurrentVersion(string projectFile, out Version? version)
    {
        version = null;
        try
        {
            var regex = new Regex("^\\s*\\<AssemblyVersion\\>(?<Version>[^\\<]+)\\<");
            using var reader = new StreamReader(projectFile, Encoding.UTF8);
            var line = reader.ReadLine();
            while (line != null)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    if (Version.TryParse(match.Groups["Version"].Value, out version))
                        return PackagingResult.Success;
                    return PackagingResult.UnclassifiedError;
                }
                line = reader.ReadLine();
            }
            return PackagingResult.UnclassifiedError;
        }
        catch (Exception ex)
        {
            if (ex is IOException)
                return PackagingResult.FileOrDirectoryNotFound;
            return PackagingResult.UnclassifiedError;
        }
    }


    // Get previous version of given project.
    PackagingResult GetPreviousVersion(IList<string> args)
    {
        if (args.IsEmpty())
        {
            Console.Error.WriteLine("No project specified.");
            return PackagingResult.InvalidArgument;
        }
        var result = this.GetPreviousVersion(args[0], out var version);
        if (result == PackagingResult.Success && version != null)
            Console.Write(version);
        return result;
    }
    PackagingResult GetPreviousVersion(string projectFile, out Version? version)
    {
        // get current version
        version = null;
        var result = this.GetCurrentVersion(projectFile, out var currentVersion);
        if (result != PackagingResult.Success)
        {
            Console.Error.Write($"Unable to get current version of '{projectFile}'");
            return result;
        }

        // find previous version
        try
        {
            if (!Directory.Exists(PackagesFolderName))
                return PackagingResult.Success;
            foreach (var path in Directory.EnumerateDirectories(PackagesFolderName))
            {
                if (Version.TryParse(Path.GetFileName(path), out var candVersion)
                    && candVersion != currentVersion)
                {
                    if (version == null || candVersion > version)
                        version = candVersion;
                }
            }
        }
        catch
        { }
        return PackagingResult.Success;
    }


    /// <summary>
    /// Run tool with arguments.
    /// </summary>
    /// <param name="args">Arguments/</param>
    /// <returns>Result code.</returns>
    public PackagingResult Run(IList<string> args)
    {
        // check argument
        if (args.IsEmpty())
        {
            Console.Error.WriteLine($"No command specified.");
            return PackagingResult.InvalidArgument;
        }
        
        // run tool
        switch (args[0])
        {
            case "create-diff-packages":
                return this.CreateDiffPackages(args.GetRangeView(1, args.Count - 1));
            case "create-package-manifest":
                return this.CreatePackageManifest(args.GetRangeView(1, args.Count - 1));
            case "get-current-version":
                return this.GetCurrentVersion(args.GetRangeView(1, args.Count - 1));
            case "get-previous-version":
                return this.GetPreviousVersion(args.GetRangeView(1, args.Count - 1));
        }

        // unknown command
        Console.Error.WriteLine($"Unknown command '{args[0]}'");
        return PackagingResult.InvalidArgument;
    }
}


/// <summary>
/// Result of packaging.
/// </summary>
public enum PackagingResult
{
    /// <summary>
    /// Success.
    /// </summary>
    Success,
    /// <summary>
    /// Unclassified error.
    /// </summary>
    UnclassifiedError,
    /// <summary>
    /// At least one argument is invalid.
    /// </summary>
    InvalidArgument,
    /// <summary>
    /// One of files of directories cannot be found.
    /// </summary>
    FileOrDirectoryNotFound,
    /// <summary>
    /// Project cannot be found.
    /// </summary>
    ProjectNotFound,
}