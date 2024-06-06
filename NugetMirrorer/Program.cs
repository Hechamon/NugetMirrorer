using Cocona;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NugetMirrorer;

var app = CoconaLiteApp.Create();

app.AddCommand(async (Parameters parameters, CoconaAppContext context) =>
{
    var logger = new ConsoleLogger{MinLevel = LogLevel.Debug};
    logger.LogMinimal($"Starting: Mirroring from '{parameters.Source}' to '{parameters.Destination}'");
    var sourceRepository = Repository.Factory.GetCoreV2(new PackageSource(parameters.Source));
    var destinationRepository = Repository.Factory.GetCoreV3(parameters.Destination, FeedType.HttpV3);

    var comparer = new NugetComparer(logger, sourceRepository, destinationRepository);
    var missingPackages = comparer.Execute(parameters.Search, context.CancellationToken);

    var mover = new NugetMover(logger, sourceRepository, destinationRepository, parameters.ApiKey);

    await mover.Move(missingPackages, context.CancellationToken);

    logger.LogMinimal("Done");
});

await app.RunAsync();


