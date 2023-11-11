using System.Net.Security;
using Google.Protobuf;
using SMTSP.Communication.TransferTypes;
using SMTSP.Discovery;
using SMTSP.Exceptions;
using SMTSP.Extensions;

namespace SMTSP.Communication;

public static class CommunicationHandler
{
    private static FileTransfer HandleFileTransferRequest(TransferRequest transferRequest, Stream stream)
    {
        var fileTransferIntent = FileTransferIntent.Parser.ParseDelimitedFrom(stream);

        return new FileTransfer(transferRequest.Device, stream, fileTransferIntent.FileInfo.ToArray());
    }

    private static ClipboardTransfer HandleClipboardTransferRequest(TransferRequest transferRequest, Stream stream)
    {
        var clipboardTransferIntent = ClipboardTransferIntent.Parser.ParseDelimitedFrom(stream);

        return new ClipboardTransfer(transferRequest.Device, stream, clipboardTransferIntent.ClipboardContent.ToString() ?? "");
    }

    public static TransferBase GetTransferRequest(SslStream stream, CancellationToken cancellationToken = default)
    {
        var transferRequest = TransferRequest.Parser.ParseDelimitedFrom(stream);

        TransferBase transferBase = transferRequest.Intent switch
        {
            TransferRequest.Types.CommunicationIntents.FileTransfer => HandleFileTransferRequest(transferRequest, stream),
            TransferRequest.Types.CommunicationIntents.ClipboardTransfer => HandleClipboardTransferRequest(transferRequest, stream),
            _ => throw new ArgumentOutOfRangeException($"No suitable implementation found for connection intent {transferRequest.Intent}")
        };

        return transferBase;
    }

    /// <summary>
    /// Transfer one or multiple files.
    /// </summary>
    /// <param name="myDevice"></param>
    /// <param name="encryptedPeerStream"></param>
    /// <param name="fileInfos"></param>
    /// <param name="fileStream">The Stream to the file or zip.</param>
    /// <param name="progress"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="TransferDeniedException">Occurs, when the participant denied the transfer request.</exception>
    public static async Task TransferFiles(Device myDevice, SslStream encryptedPeerStream, IEnumerable<SharedFileInfo> fileInfos, Stream fileStream, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        var transferRequest = new TransferRequest
        {
            Device = myDevice,
            Intent = TransferRequest.Types.CommunicationIntents.FileTransfer
        };

        transferRequest.WriteDelimitedTo(encryptedPeerStream);

        var fileTransferIntent = new FileTransferIntent
        {
            FileInfo = { fileInfos }
        };

        fileTransferIntent.WriteDelimitedTo(encryptedPeerStream);

        var response = TransferRequestResponse.Parser.ParseDelimitedFrom(encryptedPeerStream);

        if (response.Answer == TransferRequestResponse.Types.Answers.Deny)
        {
            throw new TransferDeniedException();
        }

        await fileStream.CopyToAsyncWithProgress(encryptedPeerStream, progress, cancellationToken);
        fileStream.Close();
        await fileStream.DisposeAsync();
    }
}
