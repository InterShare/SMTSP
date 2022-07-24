namespace SMTSP.Core;

/// <summary>
/// To configure basic things.
/// </summary>
public class SmtsConfiguration
{
    internal static ushort ProtocolVersion => 1;

    /// <summary>
    /// Enables logger output in console
    /// </summary>
    public static bool LoggerOutputEnabled => false;
}