using Google.Protobuf;
using SMTSP.Communication.TransferTypes;
using SMTSP.Encryption;
using SMTSP.Exceptions;
using SMTSP.Extensions;
using SMTSP.Protocol.Communication;
using SMTSP.Protocol.Discovery;

namespace SMTSP.Communication;

public static class CommunicationHandler
{
    public static async Task<Stream> EncryptStream(Stream unencryptedStream, CancellationToken cancellationToken = default)
    {
        var sessionEncryption = new SessionEncryption();
        var publicKey = sessionEncryption.GetMyPublicKey();

        var encryptionRequest = new EncryptionRequest
        {
            PublicKey = ByteString.CopyFrom(publicKey)
        }.ToByteArray();

        await unencryptedStream.WriteAsync(encryptionRequest, cancellationToken);

        var response = EncryptionRequestResponse.Parser.ParseFrom(unencryptedStream);

        var aesKey = sessionEncryption.CalculateAesKey(response.PublicKey.ToByteArray());

        Stream cryptoStream = SessionEncryption.CreateCryptoStream(unencryptedStream, aesKey, response.Iv.ToByteArray());

        return cryptoStream;
    }

    private static async Task<Stream> EncryptForeignStream(Stream unencryptedStream, CancellationToken cancellationToken = default)
    {
        var sessionEncryption = new SessionEncryption();
        var publicKey = sessionEncryption.GetMyPublicKey();
        var iv = SessionEncryption.GenerateIvBytes();

        var encryptionRequest = EncryptionRequest.Parser.ParseFrom(unencryptedStream);

        var response = new EncryptionRequestResponse
        {
            PublicKey = ByteString.CopyFrom(publicKey),
            Iv = ByteString.CopyFrom(iv)
        }.ToByteArray();

        await unencryptedStream.WriteAsync(response, cancellationToken);

        var aesKey = sessionEncryption.CalculateAesKey(encryptionRequest.PublicKey.ToByteArray());

        Stream cryptoStream = SessionEncryption.CreateCryptoStream(unencryptedStream, aesKey, iv);

        return cryptoStream;
    }

    private static FileTransfer HandleFileTransferRequest(TransferRequest transferRequest, Stream stream)
    {
        var fileTransferIntent = FileTransferIntent.Parser.ParseFrom(stream);

        return new FileTransfer(transferRequest.Device, stream, fileTransferIntent.FileInfo.ToArray());
    }

    private static ClipboardTransfer HandleClipboardTransferRequest(TransferRequest transferRequest, Stream stream)
    {
        var clipboardTransferIntent = ClipboardTransferIntent.Parser.ParseFrom(stream);

        return new ClipboardTransfer(transferRequest.Device, stream, clipboardTransferIntent.ClipboardContent.ToString() ?? "");
    }

    public static async Task<TransferBase> EstablishSecureConnectionToIncomingDataStream(Stream stream, CancellationToken cancellationToken = default)
    {
        await using var encryptedStream = await EncryptForeignStream(stream, cancellationToken);
        var transferRequest = TransferRequest.Parser.ParseFrom(encryptedStream);

        TransferBase transferBase = transferRequest.Intent switch
        {
            TransferRequest.Types.CommunicationIntents.FileTransfer => HandleFileTransferRequest(transferRequest, encryptedStream),
            TransferRequest.Types.CommunicationIntents.ClipboardTransfer => HandleClipboardTransferRequest(transferRequest, encryptedStream),
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
    public static async Task TransferFiles(Device myDevice, Stream encryptedPeerStream, IEnumerable<SharedFileInfo> fileInfos, Stream fileStream, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        var transferRequest = new TransferRequest
        {
            Device = myDevice,
            Intent = TransferRequest.Types.CommunicationIntents.FileTransfer
        };

        await encryptedPeerStream.WriteAsync(transferRequest.ToByteArray(), cancellationToken);

        var fileTransferIntent = new FileTransferIntent
        {
            FileInfo = { fileInfos }
        };

        await encryptedPeerStream.WriteAsync(fileTransferIntent.ToByteArray(), cancellationToken);

        var response = TransferRequestResponse.Parser.ParseFrom(encryptedPeerStream);

        if (response.Answer == TransferRequestResponse.Types.Answers.Deny)
        {
            throw new TransferDeniedException();
        }

        await fileStream.CopyToAsyncWithProgress(encryptedPeerStream, progress, cancellationToken);
    }
}
