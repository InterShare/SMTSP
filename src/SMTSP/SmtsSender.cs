using System.Net.Sockets;
using SMTSP.Core;
using SMTSP.Entities;
using SMTSP.Extensions;

namespace SMTSP;

/// <summary>Class <c>SmtsSender</c> can be used to send data to other devices</summary>
public class SmtsSender
{
    /// <summary>
    /// Send data to a peripheral
    /// </summary>
    /// <param name="receiver"></param>
    /// <param name="file"></param>
    /// <param name="myDeviceInfo"></param>
    /// <param name="progress"></param>
    /// <param name="cancellationToken"></param>
    public static async Task<SendFileResponses> SendFile(DeviceInfo receiver, SmtsFile file, DeviceInfo myDeviceInfo, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = new TcpClient(receiver.IpAddress, receiver.TransferPort);

            using NetworkStream tcpStream = client.GetStream();

            var transferRequest = new TransferRequest
            {
                Id = Guid.NewGuid().ToString(),
                FileName = file.Name,
                SenderId = myDeviceInfo.DeviceId,
                SenderName = myDeviceInfo.DeviceName,
                FileSize = file.FileSize
            };

            byte[] binaryTransferRequest = transferRequest.ToBinary();

            await tcpStream.WriteAsync(binaryTransferRequest, 0, binaryTransferRequest.Length, cancellationToken);

            var response = new byte[7];
            await tcpStream.ReadAsync(response, 0, response.Length, cancellationToken);

            Enum.TryParse(response.GetStringFromBytes(), true, out TransferRequestAnswers responseAnswer);
            Logger.Info($"Received response answer: {responseAnswer}");

            if (responseAnswer == TransferRequestAnswers.Accept)
            {
                await file.DataStream.CopyToAsyncWithProgress(tcpStream, progress, cancellationToken);

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