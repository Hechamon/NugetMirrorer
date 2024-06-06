using System.Runtime.CompilerServices;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.Versioning;
using NugetMirrorer.Extensions;

namespace NugetMirrorer;

internal sealed class NugetComparer
{
    private readonly SourceCacheContext _sourceCacheContext = new();
    private readonly ILogger _logger;
    private readonly SourceRepository _sourceRepository;
    private readonly SourceRepository _destinationRepository;

    public NugetComparer(ILogger logger, SourceRepository sourceRepository, SourceRepository destinationRepository)
    {
        _logger = logger;
        _sourceRepository = sourceRepository;
        _destinationRepository = destinationRepository;
    }

    public async IAsyncEnumerable<(string Id, NuGetVersion Version)> Execute(string? search, [EnumeratorCancellation] CancellationToken ct)
    {
        search ??= string.Empty;
        var sourceListPackages = await _sourceRepository.GetResourceAsync<ListResource>(ct);
        var destinationPackages = await _destinationRepository.GetResourceAsync<MetadataResource>(ct);

        if (sourceListPackages is null)
        {
            _logger.LogError("Source package feed does not support listing packages.");
            yield break;
        }

        if (destinationPackages is null)
        {
            _logger.LogError("Destination package source does not support getting package metadata");
            yield break;
        }

        IEnumerableAsync<IPackageSearchMetadata> packages;
        try
        {
            packages = await sourceListPackages.ListAsync(
                search,
                true,
                true,
                true,
                _logger,
                ct);
        }
        catch (Exception ex) when (ex is NuGetProtocolException or ProtocolException)
        {
            _logger.LogError($"Could not get packages from source feed: {ex.Message}");
            yield break;
        }

        await foreach (var package in packages)
        {
            var sourceVersions = await package.GetVersionsAsync();

            IEnumerable<NuGetVersion> destinationVersion;
            try
            {
                destinationVersion = await destinationPackages.GetVersions(package.Identity.Id, true, true, _sourceCacheContext, _logger,ct);
            }
            catch (Exception ex) when (ex is NuGetProtocolException or ProtocolException)
            {
                _logger.LogError($"Could not get version of package {package.Identity.Id} from destination");
                continue;
            }

            if (sourceVersions is null) continue;

            foreach (var version in sourceVersions.Select(v => v.Version).Except(destinationVersion))
            {
                if(version is not null)
                    yield return (package.Identity.Id, version);
            }
        }
    }
}