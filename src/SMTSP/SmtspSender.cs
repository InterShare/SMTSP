using System.Net.Sockets;
using SMTSP.Core;
using SMTSP.Entities;
using SMTSP.Entities.Content;
using SMTSP.Extensions;

namespace SMTSP;

/// <summary>Class <c>SmtsSender</c> can be used to send data to other devices</summary>
public class SmtspSender
{
    /// <summary>
    /// Send data to a peripheral
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="contentBase"></param>
    /// <param name="myDeviceInfo"></param>
    /// <param name="progress"></param>
    /// <param name="cancellationToken"></param>
    public static async Task<SendFileResponses> SendFile(DeviceInfo receiver, SmtspContentBase contentBase, DeviceInfo myDeviceInfo, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new TcpClient(receiver.IpAddress, receiver.Port);

            await using NetworkStream tcpStream = client.GetStream();

            var encryption = new Encryption.Encryption();

            var transferRequest = new TransferRequest
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = myDeviceInfo.DeviceId,
                SenderName = myDeviceInfo.DeviceName,
                ContentBase = contentBase,
                PublicKey = encryption.GetMyPublicKey()
            };

            byte[] binaryTransferRequest = transferRequest.ToBinary();

            await tcpStream.WriteAsync(binaryTransferRequest, cancellationToken);

            byte[] response = new byte[7];
            // ReSharper disable once MustUseReturnValue
            await tcpStream.ReadAsync(response, cancellationToken);

            var responseAnswer = response.GetStringFromBytes().ToEnum<TransferRequestAnswers>();
            Logger.Info($"Received response answer: {responseAnswer}");

            if (responseAnswer == TransferRequestAnswers.Accept)
            {
                byte[] foreignPublicKeyBytes = TransferRequest.GetPublicKey(tcpStream);

                byte[] aesKey = encryption.CalculateAesKey(foreignPublicKeyBytes);
                byte[] iv = Encryption.Encryption.GenerateIvBytes();
                byte[] ivBase64 = Convert.ToBase64String(iv).GetBytes().ToArray();
                await tcpStream.WriteAsync(ivBase64, cancellationToken);

                await encryption.EncryptStream(tcpStream, contentBase.DataStream!, aesKey, iv, progress, cancellationToken);

                // await contentBase.DataStream!.CopyToAsyncWithProgress(tcpStream, progress, cancellationToken);

                Logger.Info("Done sending");
                return SendFileResponses.Success;
            }

            tcpStream.Close();
            return SendFileResponses.Denied;
        }
        catch (OperationCanceledException exception)
        {
        }
        catch (IOException exception)
        {
            if (exception.Message.Contains("Connection reset by peer"))
            {
                return SendFileResponses.Corrupted;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }

        return SendFileResponses.Unknown;
    }
}