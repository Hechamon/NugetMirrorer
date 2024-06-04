using Cocona;
using NugetMirrorer;

var app = CoconaLiteApp.Create();

app.AddCommand(async (string source, string destination, string? search, bool dryRun) =>
{
    var comparer = new NugetComparer(source, destination, search);

    var missingPackages = await comparer.Execute();

    foreach (var (name, versions) in missingPackages)
    {
        Console.Write($"{name}: ");
        Console.WriteLine(string.Join(',', versions));
    }

    Console.WriteLine("Done");
});

await app.RunAsync();


