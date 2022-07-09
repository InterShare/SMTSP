using SMTSP.Entities;
using SMTSP.Entities.Content;

namespace SMTSP.Communication;

/// <summary>
/// The base interface which all communication-services implement.
/// </summary>
public interface ICommunication : IDisposable
{
    /// <summary>
    /// Is triggered, when a new connection is established.
    /// </summary>
    event EventHandler<Stream> OnReceive;

    /// <summary>
    /// Start the communication service.
    /// </summary>
    /// <returns></returns>
    Task Start();

    /// <summary>
    /// Send data to a peripheral.
    /// </summary>
    /// <param name="receiver"></param>
    public Stream ConnectToDevice(DeviceInfo receiver);

}