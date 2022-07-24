using SMTSP.Communication.Implementation.Tcp;

namespace SMTSP.Communication;

/// <summary>
/// Manages, which communication implementation will be used.
/// </summary>
public static class CommunicationManager
{
    /// <summary>
    /// Holds the list of communication implementations. Will be used in order of 
    /// </summary>
    public static List<ICommunication> CommunicationImplementations { get; } = new()
    {
        new TcpCommunication()
    };

    /// <summary>
    /// Gets the most suitable communication implementation.
    /// </summary>
    /// <returns></returns>
    public static ICommunication GetMostSuitableImplementation()
    {
        // TODO
        return CommunicationImplementations.First();
    }
}