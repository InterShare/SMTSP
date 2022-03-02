namespace SMTSP.Core;

/// <summary>
/// To configure basic things.
/// </summary>
public class SmtsConfig
{
    internal const string ServiceName = "_smtsp._tcp";
    internal static ushort ProtocolVersion => 1;

    /// <summary>
    /// Enables logger output in console
    /// </summary>
    public static bool LoggerOutputEnabled { get; set; } = false;
}