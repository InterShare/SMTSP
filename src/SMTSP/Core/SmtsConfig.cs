namespace SMTSP.Core;

/// <summary>
/// To configure basic things.
/// </summary>
public class SmtsConfig
{
    internal static string ProtocolVersion => "0.2.0";

    /// <summary>
    /// Enables logger output in console
    /// </summary>
    public static bool LoggerOutputEnabled { get; set; } = false;
}