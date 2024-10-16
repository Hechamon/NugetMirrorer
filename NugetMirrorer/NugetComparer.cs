using System.Runtime.CompilerServices;
using NuGet.Common;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.Versioning;
using NugetMirrorer.Extensions;

namespace NugetMirrorer;

internal sealed class NugetComparer
{
    private readonly SourceCacheContext _sourceCacheContext = new()
    {
        NoCache = true,
        DirectDownload = true
    };

    private readonly ILogger _logger;
    private readonly SourceRepository _sourceRepository;
    private readonly SourceRepository _destinationRepository;

    public NugetComparer(ILogger logger, SourceRepository sourceRepository, SourceRepository destinationRepository)
    {
        _logger = logger;
        _sourceRepository = sourceRepository;
        _destinationRepository = destinationRepository;
    }

    public async IAsyncEnumerable<(string Id, NuGetVersion Version)> Execute(
        string? search,
        int? maxAgeDays,
        bool expandDependencies,
        [EnumeratorCancellation] CancellationToken ct)
    {
        search ??= string.Empty;
        DateTimeOffset? earliestPublishDate =
            maxAgeDays.HasValue ? DateTimeOffset.Now.AddDays(-maxAgeDays.Value) : null;

        var sourceListPackages = await _sourceRepository.GetResourceAsync<ListResource>(ct);
        if (sourceListPackages is null)
        {
            _logger.LogError("Source package feed does not support listing packages.");
            yield break;
        }

        MetadataResource? sourceMetadataResource = null;
        PackageMetadataResource? sourcePackageMetadataResource = null;
        if (expandDependencies)
        {
            sourceMetadataResource = await _sourceRepository.GetResourceAsync<MetadataResource>(ct);
            if (sourceMetadataResource is null)
            {
                _logger.LogError("Source package feed does not support getting metadata.");
                yield break;
            }
            sourcePackageMetadataResource = await _sourceRepository.GetResourceAsync<PackageMetadataResource>(ct);
            if (sourcePackageMetadataResource is null)
            {
                _logger.LogError("Source package feed does not support getting package metadata.");
                yield break;
            }
        }

        var destinationPackages = await _destinationRepository.GetResourceAsync<MetadataResource>(ct);
        if (destinationPackages is null)
        {
            _logger.LogError("Destination package source does not support getting package metadata");
            yield break;
        }

        IAsyncEnumerable<IPackageSearchMetadata> packages;
        try
        {
            packages = (await sourceListPackages.ListAsync(
                search,
                false,
                true,
                true,
                _logger,
                ct)).AsAsyncEnumerable();
        }
        catch (Exception ex) when (ex is NuGetProtocolException or ProtocolException)
        {
            _logger.LogError($"Could not get packages from source feed: {ex.Message}");
            yield break;
        }

        if (expandDependencies)
        {
            packages = packages.SelectMany(p =>
                ExpandWithDependencies(sourcePackageMetadataResource!, sourceMetadataResource!, p, ct: ct));
        }

        await foreach (var package in packages.WithCancellation(ct))
        {
            var sourceVersions = await package.GetVersionsAsync();

            if (sourceVersions is null) continue;

            IEnumerable<NuGetVersion> destinationVersion;
            try
            {
                destinationVersion = await destinationPackages.GetVersions(
                    package.Identity.Id,
                    true,
                    true,
                    _sourceCacheContext,
                    _logger,
                    ct);
            }
            catch (Exception ex) when (ex is NuGetProtocolException or ProtocolException)
            {
                _logger.LogError($"Could not get version of package {package.Identity.Id} from destination");
                continue;
            }

            if (earliestPublishDate is not null)
            {
                sourceVersions = sourceVersions.Where(v => v.PackageSearchMetadata.Published >= earliestPublishDate);
            }

            foreach (var version in sourceVersions
                         .Select(v => v.Version).Except(destinationVersion)
                         .Where(v => v is not null))
            {
                yield return (package.Identity.Id, version);
            }
        }
    }

    private async IAsyncEnumerable<IPackageSearchMetadata> ExpandWithDependencies(
        PackageMetadataResource sourcePackagesMetadata,
        MetadataResource sourceMetadata,
        IPackageSearchMetadata package,
        short maxDepth = 5,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (maxDepth <= 0)
        {
            yield break;
        }

        // return the original package
        yield return package;

        // get all the dependencies
        var dependencies = package.DependencySets.SelectMany(ds => ds.Packages).Distinct();

        foreach (var dependency in dependencies)
        {
            var versions = (await sourceMetadata.GetVersions(dependency.Id, _sourceCacheContext, _logger, ct)).ToList();
            var bestMatch = dependency.VersionRange.FindBestMatch(versions) ?? versions.FirstOrDefault();

            if (bestMatch is null) continue;

            var packagesWithDependencies = ExpandWithDependencies(
                sourcePackagesMetadata,
                sourceMetadata,
                await sourcePackagesMetadata.GetMetadataAsync(
                    new PackageIdentity(dependency.Id, bestMatch),
                    _sourceCacheContext, _logger, ct),
                (short)(maxDepth - 1),
                ct);

            await foreach (var packageSearchMetadata in packagesWithDependencies)
            {
                yield return packageSearchMetadata;
            }
        }
    }
}