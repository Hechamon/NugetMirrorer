using Cocona;
using NuGet.Common;

namespace NugetMirrorer;

internal sealed record Parameters(
    string Source,
    string Destination,
    string? Search,
    string? ApiKey,
    bool DryRun,
    int? MaxAgeDays = null,
    bool WithDependencies = true,
    LogLevel LogLevel = LogLevel.Minimal) : ICommandParameterSet;