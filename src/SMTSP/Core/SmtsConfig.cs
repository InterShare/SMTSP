namespace SMTSP.Core;

public class SmtsConfig
{
    internal static string ProtocolVersion => "0.2.0";
    public static int DefaultPort => 42420;
    public static bool LoggerOutputEnabled { get; set; } = false;
}