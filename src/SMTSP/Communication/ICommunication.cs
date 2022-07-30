using SMTSP.Entities;

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
    Task Start(DeviceInfo deviceInfo);

    /// <summary>
    /// Send data to a peripheral.
    /// </summary>
    /// <param name="receiver"></param>
    Stream ConnectToDevice(DeviceInfo receiver);
}