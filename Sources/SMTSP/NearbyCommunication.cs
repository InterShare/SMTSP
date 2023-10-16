using SMTSP.Communication;
using SMTSP.Communication.TransferTypes;
using SMTSP.Core;
using SMTSP.Exceptions;
using SMTSP.Protocol.Communication;
using SMTSP.Protocol.Discovery;

namespace SMTSP;

public class NearbyCommunication
{
    private readonly Device _device;

    public event EventHandler<TransferBase> OnConnectionRequest = delegate { };

    public NearbyCommunication(Device myDevice)
    {
        _device = myDevice;
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
                await communicationService.Start(_device);
                communicationService.OnReceive += OnReceive;
            }
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
        }
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
        var unencryptedStream = mostSuitableImplementation.ConnectToDevice(recipient);
        var encryptedStream = await CommunicationHandler.EncryptStream(unencryptedStream, cancellationToken);

        await CommunicationHandler.TransferFiles(
            _device,
            encryptedStream,
            new [] { fileInfo },
            fileStream,
            progress,
            cancellationToken
        );
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
    private async void OnReceive(object? sender, Stream stream)
    {
        var request = await CommunicationHandler.EstablishSecureConnectionToIncomingDataStream(stream);
        OnConnectionRequest.Invoke(this, request);
    }
}
