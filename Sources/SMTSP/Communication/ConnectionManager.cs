using SMTSP.Communication.Backends;

namespace SMTSP.Communication;


/// <summary>
/// Manages, which communication implementation will be used.
/// </summary>
public static class ConnectionManager
{
    /// <summary>
    /// Holds the list of communication implementations. Will be used in order of
    /// </summary>
    public static List<ICommunicationBackend> CommunicationImplementations { get; } = new()
    {
        new TcpCommunicationBackend()
    };

    /// <summary>
    /// Gets the most suitable communication implementation.
    /// </summary>
    /// <returns></returns>
    public static ICommunicationBackend GetMostSuitableImplementation()
    {
        // TODO
        return CommunicationImplementations.First();
    }
}
