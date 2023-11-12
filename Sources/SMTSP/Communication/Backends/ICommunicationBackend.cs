using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SMTSP.Discovery;

namespace SMTSP.Communication.Backends;


/// <summary>
/// The base interface which all communication-services implement.
/// </summary>
public interface ICommunicationBackend : IDisposable
{
    /// <summary>
    /// Is triggered, when a new connection is established.
    /// </summary>
    event EventHandler<SslStream> OnReceive;

    /// <summary>
    /// Start the communication service.
    /// </summary>
    /// <returns></returns>
    Task Start(Device deviceInfo, X509Certificate2 certificate);

    /// <summary>
    /// Send data to a peripheral.
    /// </summary>
    /// <param name="receiver"></param>
    Task<(SslStream, IDisposable)> ConnectToDevice(Device receiver);
}
