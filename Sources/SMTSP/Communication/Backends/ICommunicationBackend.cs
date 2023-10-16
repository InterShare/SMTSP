using SMTSP.Protocol.Discovery;

namespace SMTSP.Communication.Backends;


/// <summary>
/// The base interface which all communication-services implement.
/// </summary>
public interface ICommunicationBackend : IDisposable
{
    /// <summary>
    /// Is triggered, when a new connection is established.
    /// </summary>
    event EventHandler<Stream> OnReceive;

    /// <summary>
    /// Start the communication service.
    /// </summary>
    /// <returns></returns>
    Task Start(Device deviceInfo);

    /// <summary>
    /// Send data to a peripheral.
    /// </summary>
    /// <param name="receiver"></param>
    Stream ConnectToDevice(Device receiver);
}
