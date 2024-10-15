using NuGet.Common;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Plugins;
using NuGet.Versioning;

namespace NugetMirrorer;

internal sealed class NugetMover
{
    private readonly ILogger _logger;
    private readonly SourceRepository _sourceRepository;
    private readonly SourceRepository _destinationRepository;
    private readonly string? _apiKey;
    private readonly SourceCacheContext _sourceCacheContext = new();

    public NugetMover(ILogger logger, SourceRepository sourceRepository, SourceRepository destinationRepository,
        string? apiKey)
    {
        _logger = logger;
        _sourceRepository = sourceRepository;
        _destinationRepository = destinationRepository;
        _apiKey = apiKey;
    }

    public async Task<bool> Move(IAsyncEnumerable<(string Id, NuGetVersion Version)> packages, bool dryRun,
        CancellationToken ct)
    {
        FindPackageByIdResource source;
        try
        {
            source = await _sourceRepository.GetResourceAsync<FindPackageByIdResource>(ct);
        }
        catch (Exception ex) when (ex is NuGetProtocolException or ProtocolException)
        {
            _logger.LogError($"Source does not support finding packages or failed to connect: {ex.Message}");
            return false;
        }

        PackageUpdateResource destination;
        try
        {
            destination = await _destinationRepository.GetResourceAsync<PackageUpdateResource>(ct);
        }
        catch (Exception ex) when (ex is NuGetProtocolException or ProtocolException)
        {
            _logger.LogError($"Destination does not support pushing packages or failed to connect: {ex.Message}");
            return false;
        }

        await foreach (var (id, version) in packages.WithCancellation(ct))
        {
            _logger.LogMinimal($"Moving package '{id}', version '{version.ToString()}'");

            if(dryRun) continue;

            var tempFileName = Path.GetTempFileName();
            try
            {
                await using (var packageStream = File.Create(tempFileName))
                {
                    await source.CopyNupkgToStreamAsync(id, version, packageStream, _sourceCacheContext, _logger, ct);
                }

                await destination.Push([tempFileName], null, 5 * 60, false, _ => _apiKey, _ => null,
                    false, true, null, false,
                    _logger);

            }
            catch (Exception ex) when (ex is NuGetProtocolException or ProtocolException)
            {
                _logger.LogError($"Download or upload of {id} failed: {ex.Message}");
            }
            finally
            {
                if(File.Exists(tempFileName)) File.Delete(tempFileName);
            }
        }

        return true;
    }
}