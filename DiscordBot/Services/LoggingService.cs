namespace DiscordBot.Services;

public static class LoggingService
{
    public static Task Log(LogMessage message)
    {
        if (message.Source == "Victoria" && message.Severity == LogSeverity.Debug) return Task.CompletedTask;
        
        switch (message.Severity)
        {
            case LogSeverity.Critical:
            case LogSeverity.Error:
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            case LogSeverity.Warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case LogSeverity.Verbose:
            case LogSeverity.Debug:
                Console.ForegroundColor = ConsoleColor.DarkGray;
                break;
            case LogSeverity.Info:
            default:
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }

        Console.WriteLine(
            $"{DateTime.Now,-19} [{message.Severity,8}] {message.Source}: {message.Message} {message.Exception}");
        Console.ResetColor();
        
        return Task.CompletedTask;
    }

    public static Task LogMessage(string source, string message)
    {
        var log = new LogMessage(LogSeverity.Info, source, message);
        return Log(log);
    }

    public static Task LogMusicMessage(string source, string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"{DateTime.Now,-19} [{"Music",8}] {source}: {message}");
        Console.ResetColor();
        
        return Task.CompletedTask;
    }

    public static Task LogError(string source, string message, Exception exception)
    {
        var log = new LogMessage(LogSeverity.Error, source, message, exception);
        return Log(log);
    }
}