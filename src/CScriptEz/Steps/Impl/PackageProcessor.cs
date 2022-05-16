using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
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
        private const string DefaultAddress = "https://api.nuget.org/v3/index.json";

        public PackageProcessor(ILoggerFactory loggerFactory) : base(loggerFactory.CreateLogger<PackageProcessor>())
        {
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
                    ProcessPackage(localPackage, context.PreprocessorResult.Libraries);
                }
                else
                {
                    Log($"Cannot get installed package: {packageId}, address {address}. Skipping package processing");
                }
            }
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
            return package;
        }

        public DownloadResourceResult InstallPackage(string packageId, string address, NuGetVersion version)
        {
            INugetLogger logger = NullLogger.Instance;
            CancellationToken cancellationToken = CancellationToken.None;

            var settings = Settings.LoadDefaultSettings(null);
            var globalPackagesFolder = SettingsUtility.GetGlobalPackagesFolder(settings);

            var cache = new SourceCacheContext();
            var repository = Repository.Factory.GetCoreV3(address);
            var resource = repository.GetResource<FindPackageByIdResource>();

            // Download the package
            using MemoryStream packageStream = new MemoryStream();
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
            return downloadResult;
        }

        private void ProcessPackage(DownloadResourceResult localPackage, List<LibraryDescriptor> libraries)
        {
            var logger = NullLogger.Instance;

            var targetFrameworkAttribute = (TargetFrameworkAttribute)Assembly.GetExecutingAssembly()
                .GetCustomAttributes(typeof(TargetFrameworkAttribute))
                .SingleOrDefault();

            var currentFrameworkName = GetCurrentFrameworkName();
            if (string.IsNullOrWhiteSpace(currentFrameworkName))
            {
                Log($"Cannot identify current framework. Exiting");
                return;
            }

            var currentFramework =
                NuGetFramework.ParseComponents(targetFrameworkAttribute.FrameworkName, null);

            Log($"Current framework: {currentFramework.Framework}, version: {currentFramework.Version}");

            var dependencySet = localPackage.PackageReader.NuspecReader
                .GetDependencyGroups()
                .GetNearest(currentFramework);

            if (dependencySet == null)
            {
                Log($"Cannot identify nearest matching framework for current framework {currentFramework}. Exiting");
                return;
            }

            Log($"Nearest matching framework is: {dependencySet.TargetFramework}");

            var group = localPackage.PackageReader.GetLibItems()
                .FirstOrDefault(x => x.TargetFramework == dependencySet.TargetFramework);

            if (group == null)
            {
                Log($"Cannot find files group for framework: {dependencySet.TargetFramework}. Exiting");
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
                CopyFile(localPackage.PackageReader.GetStream(groupItem), fileName);
                libraries.Add(new LibraryDescriptor(fileName, true));
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
    }
}