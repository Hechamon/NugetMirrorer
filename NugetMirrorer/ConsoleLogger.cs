using NuGet.Common;

namespace NugetMirrorer;

internal sealed class ConsoleLogger : ILogger
{
    public LogLevel MinLevel { get; init; } = LogLevel.Information;

    public void LogDebug(string data)
    {
        Log(LogLevel.Debug, data);
    }

    public void LogVerbose(string data)
    {
        Log(LogLevel.Verbose, data);
    }

    public void LogInformation(string data)
    {
        Log(LogLevel.Information, data);
    }

    public void LogMinimal(string data)
    {
        Log(LogLevel.Minimal, data);
    }

    public void LogWarning(string data)
    {
        Log(LogLevel.Warning, data);
    }

    public void LogError(string data)
    {
        Log(LogLevel.Error, data);
    }

    public void LogInformationSummary(string data)
    {
        Log(LogLevel.Information, data);
    }

    public void Log(LogLevel level, string data)
    {
        if (level < MinLevel) return;
        if (level == LogLevel.Error)
        {
            Console.Error.WriteLine($"{level}: {data}");
            return;
        }
        Console.WriteLine($"{level}: {data}");
    }

    public async Task LogAsync(LogLevel level, string data)
    {
        Log(level, data);
    }

    public void Log(ILogMessage message)
    {
        Log(message.Level, message.Message);
    }

    public async Task LogAsync(ILogMessage message)
    {
        Log(message);
    }
}