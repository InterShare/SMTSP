using System.Net.Sockets;
using SMTSP.Core;
using SMTSP.Encryption;
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
    public static async Task<SendResponses> Send(DeviceInfo receiver, SmtspContentBase contentBase, DeviceInfo myDeviceInfo, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new TcpClient(receiver.IpAddress, receiver.TcpPort);
            await using NetworkStream tcpStream = client.GetStream();

            var encryption = new SessionEncryption();

            var transferRequest = new TransferRequest(
                Guid.NewGuid().ToString(),
                myDeviceInfo.DeviceId,
                myDeviceInfo.DeviceName,
                contentBase,
                encryption.GetMyPublicKey()
            );

            byte[] binaryTransferRequest = transferRequest.ToBinary();

            await tcpStream.WriteAsync(binaryTransferRequest, cancellationToken);

            byte[] response = new byte[7];
            // ReSharper disable once MustUseReturnValue
            await tcpStream.ReadAsync(response, cancellationToken);

            var responseAnswer = response.GetStringFromBytes().ToEnum<TransferRequestAnswers>();
            Logger.Info($"Received response answer: {responseAnswer}");

            if (responseAnswer == TransferRequestAnswers.Accept)
            {
                byte[] foreignPublicKey = new byte[67];
                // ReSharper disable once MustUseReturnValue
                await tcpStream.ReadAsync(foreignPublicKey, cancellationToken);

                Console.WriteLine($"Public Key {foreignPublicKey.Length}");

                byte[] aesKey = encryption.CalculateAesKey(foreignPublicKey);
                byte[] iv = SessionEncryption.GenerateIvBytes();
                var ivBase64 = Convert.ToBase64String(iv).GetBytes().ToList();
                ivBase64.Add(0x00);

                await tcpStream.WriteAsync(ivBase64.ToArray(), cancellationToken);

                await SessionEncryption.EncryptStream(tcpStream, contentBase.DataStream!, aesKey, iv, progress, cancellationToken);

                Logger.Info("Done sending");
                return SendResponses.Success;
            }

            tcpStream.Close();
            return SendResponses.Denied;
        }
        catch (OperationCanceledException)
        {
            // Do nothing.
        }
        catch (IOException exception)
        {
            if (exception.Message.Contains("Connection reset by peer"))
            {
                return SendResponses.Corrupted;
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw;
        }

        return SendResponses.Unknown;
    }
}