using Cocona;

namespace NugetMirrorer;

internal sealed record Parameters(string Source, string Destination, string? Search, string ApiKey, bool DryRun) : ICommandParameterSet;