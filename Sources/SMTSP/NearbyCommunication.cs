using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using SMTSP.Communication;
using SMTSP.Communication.TransferTypes;
using SMTSP.Core;
using SMTSP.Discovery;
using SMTSP.Exceptions;

namespace SMTSP;

public class NearbyCommunication
{
    private readonly Device _device;
    private readonly X509Certificate2 _certificate;
    // private readonly BonjourAdvertisement _bonjourAdvertisement;
    private readonly BleAdvertisement _bleAdvertisement;

    public event EventHandler<TransferBase> OnConnectionRequest = delegate { };

    public NearbyCommunication(Device myDevice, X509Certificate2 certificate)
    {
        _device = myDevice;
        _certificate = certificate;
        // _bonjourAdvertisement = new BonjourAdvertisement(_device);
        _bleAdvertisement = new BleAdvertisement(_device);
    }

    /// <summary>
    /// Start offering this peripheral as data receiver.
    /// </summary>
    /// <returns>Returns a <c>bool</c> to indicate whether the start succeeded.</returns>
    public async Task StartReceiving()
    {
        try
        {
            foreach (var communicationService in ConnectionManager.CommunicationImplementations)
            {
                await communicationService.Start(_device, _certificate);
                communicationService.OnReceive += OnReceive;
            }
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
        }
    }

    public void StartAdvertising()
    {
        _bleAdvertisement.StartAdvertising();
        // if (_device.TcpConnectionInfo == null || _device.TcpConnectionInfo.Port == 0)
        // {
        //     throw new NullReferenceException("TCP Port is unknown. Did you forget to start the NearbyCommunication server?");
        // }

        // _bonjourAdvertisement.Register((ushort) _device.TcpConnectionInfo.Port);
    }

    public void StopAdvertising()
    {
        _bleAdvertisement.StopAdvertising();
        // _bonjourAdvertisement.Unregister();
    }

    // public async Task SendFiles(Device recipient, ZipArchive files, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    // {
    //     var mostSuitableImplementation = ConnectionManager.GetMostSuitableImplementation();
    //     var unencryptedStream = mostSuitableImplementation.ConnectToDevice(recipient);
    //     var encryptedStream = await Communication.Communication.EncryptStream(unencryptedStream, cancellationToken);
    //
    //     var fileInfos = files.Entries.Select(file => new SharedFileInfo
    //     {
    //         FileName = file.Name,
    //         FileSize = file.Length
    //     }).ToArray();
    //
    //     await Communication.Communication.TransferFiles(
    //         _device,
    //         encryptedStream,
    //         fileInfos,
    //         files.CreateEntry("files").Open(),
    //         progress,
    //         cancellationToken
    //     );
    // }

    /// <summary>
    /// Does exactly what the name suggests. Sends a file to another device.
    /// </summary>
    /// <param name="recipient">The one, who should receive the file.</param>
    /// <param name="fileInfo">General information about the file you intend to share.</param>
    /// <param name="fileStream">The content of the file.</param>
    /// <param name="progress">Can be used to track the progress.</param>
    /// <param name="cancellationToken">Self-explanatory</param>
    public async Task SendFile(Device recipient, SharedFileInfo fileInfo, Stream fileStream, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        var mostSuitableImplementation = ConnectionManager.GetMostSuitableImplementation();
        var (encryptedStream, client) = await mostSuitableImplementation.ConnectToDevice(recipient);

        await CommunicationHandler.TransferFiles(
            _device,
            encryptedStream,
            new [] { fileInfo },
            fileStream,
            progress,
            cancellationToken
        );

        client.Dispose();
    }

    /// <summary>
    /// Stop file server and dispose all services.
    /// </summary>
    public void StopServer()
    {
        foreach (var communicationService in ConnectionManager.CommunicationImplementations)
        {
            communicationService.Dispose();
        }
    }

    /// <exception cref="TransferDeniedException">Occurs, when the participant denied the transfer request.</exception>
    private void OnReceive(object? sender, SslStream stream)
    {
        var request = CommunicationHandler.GetTransferRequest(stream);
        OnConnectionRequest.Invoke(this, request);
    }
}
