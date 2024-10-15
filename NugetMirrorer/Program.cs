using Cocona;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NugetMirrorer;

var app = CoconaLiteApp.Create();

app.AddCommand(async (Parameters parameters, CoconaAppContext context) =>
{
    var logger = new ConsoleLogger{MinLevel = parameters.LogLevel};
    logger.LogMinimal($"Starting: Mirroring from '{parameters.Source}' to '{parameters.Destination}'");

    var sourceRepository = Repository.Factory.GetCoreV3(parameters.Source);
    var destinationRepository = Repository.Factory.GetCoreV3(parameters.Destination);

    var comparer = new NugetComparer(logger, sourceRepository, destinationRepository);
    var missingPackages = comparer.Execute(parameters.Search, context.CancellationToken);

    var mover = new NugetMover(logger, sourceRepository, destinationRepository, parameters.ApiKey);

    if (!await mover.Move(missingPackages, parameters.DryRun, context.CancellationToken))
    {
        logger.LogError("Finished with error, failed to mirror packages");
        Environment.Exit(1);
    }

    logger.LogMinimal("Done");
});

await app.RunAsync();


