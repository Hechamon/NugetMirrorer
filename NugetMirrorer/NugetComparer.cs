using System.Runtime.CompilerServices;
using NuGet.Common;
using NuGet.Protocol.Core.Types;
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

        var packages = await sourceListPackages.ListAsync(
            search,
            true,
            true,
            true,
            _logger,
            ct);

        await foreach (var package in packages)
        {
            var sourceVersions = await package.GetVersionsAsync();

            var destinationVersion = await destinationPackages.GetVersions(package.Identity.Id, true, true, _sourceCacheContext, _logger,ct);

            if (sourceVersions is null) continue;

            foreach (var version in sourceVersions.Select(v => v.Version).Except(destinationVersion))
            {
                if(version is not null)
                    yield return (package.Identity.Id, version);
            }
        }
    }
}