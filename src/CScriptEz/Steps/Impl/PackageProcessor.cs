using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using Microsoft.Extensions.Logging;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Signing;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using INugetLogger = NuGet.Common.ILogger;

namespace CScriptEz.Steps.Impl
{
    public class PackageProcessor : ServiceBase, IPackageProcessor
    {
        private NuGetFramework _currentFramework;
        private const string DefaultAddress = "https://api.nuget.org/v3/index.json";
        private ConcurrentDictionary<string, IList<VersionRange>> _loadedPackages;
        private BlockingCollection<LibraryDescriptor> _libraries;

        public PackageProcessor(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<PackageProcessor>())
        {
            _loadedPackages = new ConcurrentDictionary<string, IList<VersionRange>>();
        }

        private NuGetFramework CurrentFramework
        {
            get
            {
                if (_currentFramework == null)
                {
                    var currentFrameworkName = GetCurrentFrameworkName();
                    if (string.IsNullOrWhiteSpace(currentFrameworkName))
                    {
                        Log($"Cannot identify current framework. Exiting");
                        throw new OperationCanceledException("Cannot identify current framework. Exiting");
                    }

                    _currentFramework =
                        NuGetFramework.ParseComponents(currentFrameworkName, null);
                }

                return _currentFramework;
            }
        }

        public void Run(ExecutionContext context)
        {
            LogTitle($"{nameof(PackageProcessor)}. Processing packages");
            var packages = context.PreprocessorResult.Packages;
            if (packages == null || packages.Count == 0)
            {
                Log("No packages found. Returning");
                return;
            }

            _libraries = new BlockingCollection<LibraryDescriptor>(new ConcurrentQueue<LibraryDescriptor>(context.PreprocessorResult.Libraries));

            foreach (var package in packages)
            {
                var packageId = package.Name;
                var address = CheckAddress(package.Address);
                var versionDescription =
                    string.IsNullOrWhiteSpace(package.Version) ? "latest version" : package.Version;
                Log($"Processing package: {packageId}, address: {address}, version: {versionDescription}");
                if (!PackageExists(packageId, address))
                {
                    Log($"Package {packageId} cannot be found on address {address}. Skipping package processing");
                    continue;
                }

                //if version not specified then set version to latest version
                var version = string.IsNullOrWhiteSpace(package.Version)
                    ? GetPackageLatestVersion(package.Name, address)
                    : new NuGetVersion(package.Version);


                //if version is not installed locally then install it
                var localPackage = GetInstalledPackage(packageId, version) 
                                   ?? InstallPackage(packageId, address, version);


                if (localPackage is { Status: DownloadResourceResultStatus.Available })
                {
                    CopyPackageToProgramBinaryFolder(localPackage);
                    var packageDependencyGroup = localPackage.PackageReader.GetPackageDependencies().GetNearest(CurrentFramework);
                    LoadPackageDependencyGroup(packageDependencyGroup, address);
                }
                else
                {
                    Log($"Cannot get installed package: {packageId}, address {address}. Skipping package processing");
                }
            }

            context.PreprocessorResult.Libraries.Clear();
            context.PreprocessorResult.Libraries.AddRange(_libraries);
        }

        private string CheckAddress(string address)
        {
            return string.IsNullOrWhiteSpace(address) ? DefaultAddress : address;
        }

        private bool PackageExists(string packageId, string address)
        {
            //todo find more options to check if package exists

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(address);

            var resource = repository.GetResource<FindPackageByIdResource>();
            var versions = resource.GetAllVersionsAsync(
                    packageId, cache, NullLogger.Instance, CancellationToken.None)
                .ConfigureAwait(false).GetAwaiter().GetResult().ToList();

            return versions is { Count: > 0 };
        }

        private NuGetVersion GetPackageLatestVersion(string packageId, string address)
        {
            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(address);

            var resource = repository.GetResource<FindPackageByIdResource>();
            var versions = resource.GetAllVersionsAsync(
                    packageId, cache, NullLogger.Instance, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult()
                .ToList();

            var last = versions.Last();
            return last;
        }

        public DownloadResourceResult GetInstalledPackage(string packageId, NuGetVersion version)
        {
            var settings = Settings.LoadDefaultSettings(null);
            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

            var package = GlobalPackagesFolderUtility.GetPackage(new PackageIdentity(packageId, version), globalPackagesFolder);
            Log(package == null
                ? $"Package {packageId}, version: {version} not installed."
                : $"Package {packageId}, version: {version} already installed.");

            return package;
        }

        public DownloadResourceResult InstallPackage(string packageId, string address, NuGetVersion version)
        {
            Log($"Trying to install Package {packageId}, version: {version}.");
            INugetLogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var settings = Settings.LoadDefaultSettings(null);
            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(address);
            var resource = repository.GetResource<FindPackageByIdResource>();

            // Download the package
            using var packageStream = new MemoryStream();
            resource.CopyNupkgToStreamAsync(
                packageId,
                version,
                packageStream,
                cache,
                logger,
                cancellationToken)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            packageStream.Seek(0, SeekOrigin.Begin);

            // Add it to the global package folder
            var downloadResult = GlobalPackagesFolderUtility.AddPackageAsync(
                    address,
                    new PackageIdentity(packageId, version),
                    packageStream,
                    globalPackagesFolder,
                    parentId: Guid.Empty,
                    ClientPolicyContext.GetClientPolicy(settings, logger),
                    logger,
                    cancellationToken)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            Log(downloadResult == null
                ? $"Could not install Package {packageId}, version: {version}."
                : $"Installed Package {packageId}, version: {version}.");

            return downloadResult;
        }

        private void CopyPackageToProgramBinaryFolder(DownloadResourceResult localPackage)
        {
            var logger = NullLogger.Instance;

            var currentFramework = CurrentFramework;

            Log($"Current framework: {currentFramework.Framework}, version: {currentFramework.Version}");

            // var dependencySet = localPackage.PackageReader.NuspecReader
            //     .GetDependencyGroups()
            //     .GetNearest(currentFramework);
            //
            // if (dependencySet == null)
            // {
            //     Log($"Cannot identify nearest matching framework for current framework {currentFramework}. Exiting");
            //     return;
            // }
            //
            // Log($"Nearest matching framework is: {dependencySet.TargetFramework}");

            // var group = localPackage.PackageReader.GetLibItems()
            //     .FirstOrDefault(x => x.TargetFramework == dependencySet.TargetFramework);
            //
            // if (group == null)
            // {
            //     Log($"Cannot find files group for framework: {dependencySet.TargetFramework}. Exiting");
            //     return;
            // }



            //var supportedFrameworks = localPackage.PackageReader.GetSupportedFrameworks();
            // var libFrameworks = localPackage.PackageReader.GetLibItems().Select(x => x.TargetFramework).ToList();
            var libFrameworks = localPackage.PackageReader.GetLibItems().ToList();
            // foreach (var libFramework in libFrameworks)
            // {
            //     Log($"Library Framework: {libFramework.TargetFramework}");
            // }

            var matchingLibFramework = NuGetFrameworkUtility.GetNearest(libFrameworks, currentFramework);
            if (matchingLibFramework == null)
            {
                Log($"ERROR.  Could not identify matching library framework for current framework: {currentFramework}. Check package contents");
                return;
            }
            Log($"Matching Library Framework: {matchingLibFramework.TargetFramework}");

            var group = localPackage.PackageReader.GetLibItems()
                .FirstOrDefault(x => x.TargetFramework == matchingLibFramework.TargetFramework);
            
            if (group == null)
            {
                Log($"ERROR.  Cannot find files group for framework: {matchingLibFramework.TargetFramework}. Exiting");
                return;
            }


            //var filesToCopy = new List<string>();
            foreach (var groupItem in group.Items)
            {
                Log($"Processing package file: {groupItem}");
                if (!Path.GetExtension(groupItem).EndsWith("dll"))
                {
                    Log($"Package file: {groupItem} is not dll. Skipping");
                    continue;
                }

                var fileName = Path.GetFileName(groupItem);
                var targetDir = GetDestinationDir();
                var targetFileName = fileName;// Path.GetFileName(sourcePath);
                var targetPath = Path.Combine(targetDir, targetFileName);
                if (File.Exists(targetPath))
                {
                    if(!TryRemoveExistingFile(targetPath))
                    {
                        Log($"Cannot delete existing package file {Path.GetFileName(targetPath)}. Existing file will be used. Skipping");
                        continue;
                    }
                }
                CopyFile(localPackage.PackageReader.GetStream(groupItem), targetPath);
                _libraries.Add(new LibraryDescriptor(fileName, true));
            }
        }

        private bool TryRemoveExistingFile(string filePath)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception exception)
            {
                Log($"Exception: {exception.Message}");
                return false;
            }

            return true;
        }


        private void CopyFile(Stream sourceStream, string targetPath)
        {
            Log($"Copying package file to target file: {targetPath}");
            using var targetStream = File.Create(targetPath);
            sourceStream.Seek(0, SeekOrigin.Begin);
            sourceStream.CopyTo(targetStream);
        }

        private string GetCurrentFrameworkName()
        {
            var targetFrameworkAttribute = (TargetFrameworkAttribute)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(TargetFrameworkAttribute))
                .SingleOrDefault();
            return targetFrameworkAttribute?.FrameworkName;
        }

        private string GetDestinationDir()
        {
            return Path.GetDirectoryName(typeof(Program).Assembly.Location);
        }


        private void LoadPackageDependencyGroup(PackageDependencyGroup packageDependencyGroup, string address)
        {
            if (packageDependencyGroup == null)
            {
                return;
            }
            Log($"Loading PackageDependency Group.");
            foreach (var packageDependency in packageDependencyGroup.Packages)
            {
                Log($"Loading PackageDependency item: {packageDependency}");
                LoadPackageDependency(packageDependency, address);
            }
        }

        private void LoadPackageDependency(PackageDependency packageDependency, string address)
        {
            var packageId = packageDependency.Id;
            var versionRange = packageDependency.VersionRange;
            var requiredVersion = versionRange.MinVersion;
            var isLoaded = IsPackageDependencyLoaded(packageId, versionRange);
            if (isLoaded)
            {
                Log($"PackageDependency already loaded: {packageDependency}. Skipping");
                return;
            }
            Log($"PackageDependency was not loaded: {packageDependency}. Checking if package is installed locally");

            var localPackage = GetInstalledPackage(packageId, requiredVersion)
                               ?? InstallPackage(packageId, address, requiredVersion);

            CopyPackageToProgramBinaryFolder(localPackage);
            UpdateLoadedPackages(packageId, localPackage.PackageReader.NuspecReader.GetVersion());
            var packageDependencyGroup = localPackage.PackageReader.GetPackageDependencies().GetNearest(CurrentFramework);
            LoadPackageDependencyGroup(packageDependencyGroup, address);
        }

        private void UpdateLoadedPackages(string packageId, NuGetVersion version)
        {
            if (_loadedPackages.TryGetValue(packageId, out var versions))
            {
                versions.Add(new VersionRange(version));
            }
            else
            {
                versions = new List<VersionRange>();
                versions.Add(new VersionRange(version));
                _loadedPackages[packageId] = versions;
            }
        }

        private bool IsPackageDependencyLoaded(string packageId, VersionRange versionRange)
        {
            if (!_loadedPackages.TryGetValue(packageId, out var versions))
            {
                return false;
            }
            Log($"Package was loaded {packageId}. Checking if version exists: {versionRange.MinVersion}");
            var isLoaded = versions.Any(x => x.Satisfies(versionRange.MinVersion));
            return isLoaded;
        }
    }
}