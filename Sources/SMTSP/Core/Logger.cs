namespace SMTSP.Core;

internal static class Logger
{
    public static bool OutputEnabled { get; } = true;

    private static string FormatMessage(string severity, string message)
    {
        // ReSharper disable once UseFormatSpecifierInInterpolation
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

        Console.WriteLine(FormatMessage("GOOD", message));
    }

    public static void Error(string message)
    {
        Console.WriteLine(FormatMessage("ERR", message));
    }

    public static void Warning(string message)
    {
        if (!OutputEnabled)
        {
            return;
        }

        Console.WriteLine(FormatMessage("WARN", message));
    }

    public static void System(string message)
    {
        if (!OutputEnabled)
        {
            return;
        }

        Console.WriteLine(FormatMessage("SYS", message));
    }

    public static void Exception(Exception exception)
    {
        Error(exception.ToString());
    }
}
