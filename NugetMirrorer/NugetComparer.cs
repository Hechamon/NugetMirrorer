using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NugetMirrorer.Extensions;

namespace NugetMirrorer;

internal sealed class NugetComparer
{
    private readonly string _source;
    private readonly string _destination;
    private readonly string _search;

    public NugetComparer(string source, string destination, string? search)
    {
        _destination = destination;
        _source = source;
        _search = search ?? string.Empty;
    }

    public async Task<IDictionary<string, IList<string>>> Execute()
    {
        var repository = Repository.Factory.GetCoreV2(new PackageSource(_source));

        var listPackages = await repository.GetResourceAsync<ListResource>();
        var packages = await listPackages.ListAsync(
            _search,
            true,
            true,
            true,
            new ConsoleLogger(),
            CancellationToken.None);

        var sourceList = new Dictionary<string, IList<string>>();

        await foreach (var package in packages)
        {
            var versions = await package.GetVersionsAsync();
            sourceList.Add(package.Identity.Id, versions.Select(v => v.Version.Version.ToString()).ToList());
        }
        return sourceList;
    }
}