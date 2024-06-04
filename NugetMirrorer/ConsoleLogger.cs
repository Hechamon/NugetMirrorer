using NuGet.Common;

namespace NugetMirrorer;

internal sealed class ConsoleLogger : ILogger
{
    public void LogDebug(string data)
    {
        Console.WriteLine(data);
    }

    public void LogVerbose(string data)
    {
        Console.WriteLine(data);
    }

    public void LogInformation(string data)
    {
        Console.WriteLine(data);
    }

    public void LogMinimal(string data)
    {
        Console.WriteLine(data);
    }

    public void LogWarning(string data)
    {
        Console.WriteLine(data);
    }

    public void LogError(string data)
    {
        Console.Error.WriteLine(data);
    }

    public void LogInformationSummary(string data)
    {
        Console.WriteLine(data);
    }

    public void Log(LogLevel level, string data)
    {
        if (level == LogLevel.Error)
        {
            Console.Error.WriteLine(data);
            return;
        }
        Console.WriteLine(data);
    }

    public async Task LogAsync(LogLevel level, string data)
    {
        Log(level, data);
    }

    public void Log(ILogMessage message)
    {
        Console.WriteLine(message);
    }

    public async Task LogAsync(ILogMessage message)
    {
        Log(message);
    }
}