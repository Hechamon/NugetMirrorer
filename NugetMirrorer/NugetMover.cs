using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace NugetMirrorer;

internal sealed class NugetMover
{
    private readonly ILogger _logger;
    private readonly SourceRepository _sourceRepository;
    private readonly SourceRepository _destinationRepository;
    private readonly string _apiKey;
    private readonly SourceCacheContext _sourceCacheContext = new();

    public NugetMover(ILogger logger, SourceRepository sourceRepository, SourceRepository destinationRepository,
        string apiKey)
    {
        _logger = logger;
        _sourceRepository = sourceRepository;
        _destinationRepository = destinationRepository;
        _apiKey = apiKey;
    }

    public async Task Move(IAsyncEnumerable<(string Id, NuGetVersion Version)> packages, bool dryRun,
        CancellationToken ct)
    {
        var source = await _sourceRepository.GetResourceAsync<FindPackageByIdResource>(ct);
        var destination = await _destinationRepository.GetResourceAsync<PackageUpdateResource>(ct);

        await foreach (var (id, version) in packages.WithCancellation(ct))
        {
            _logger.LogMinimal($"Moving package '{id}', version '{version.ToString()}'");

            if(dryRun) continue;

            var tempFileName = Path.GetTempFileName();
            await using (var packageStream = File.Create(tempFileName))
            {
                await source.CopyNupkgToStreamAsync(id, version, packageStream, _sourceCacheContext, _logger, ct);
            }

            await destination.Push([tempFileName], null, 5 * 60, false, _ => _apiKey, _ => null,
                false, true, null, false,
                _logger);

            File.Delete(tempFileName);
        }
    }
}