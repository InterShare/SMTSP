namespace SMTSP.Core;

internal static class Logger
{
    public static bool OutputEnabled { get; } = SmtsConfiguration.LoggerOutputEnabled;

    private static string FormatMessage(string severity, string message)
    {
        return $"{DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")} {severity} {message}";
    }

    public static void Info(string message)
    {
        if (!OutputEnabled)
        {
            return;
        }

        Console.WriteLine(FormatMessage("INFO", message));
    }

    public static void Success(string message)
    {
        if (!OutputEnabled)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(FormatMessage("GOOD", message));
        Console.ResetColor();
    }

    public static void Error(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(FormatMessage("ERR", message));
        Console.ResetColor();
    }

    public static void Warning(string message)
    {
        if (!OutputEnabled)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(FormatMessage("WARN", message));
        Console.ResetColor();
    }

    public static void System(string message)
    {
        if (!OutputEnabled)
        {
            return;
        }

        Console.ForegroundColor = ConsoleColor.Blue;
        Console.WriteLine(FormatMessage("SYS", message));
        Console.ResetColor();
    }

    public static void Exception(Exception exception)
    {
        Error(exception.ToString());
    }
}