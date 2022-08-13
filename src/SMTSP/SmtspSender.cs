using SMTSP.Communication;
using SMTSP.Core;
using SMTSP.Encryption;
using SMTSP.Entities;
using SMTSP.Entities.Content;
using SMTSP.Extensions;
using DeviceInfo = SMTSP.Entities.DeviceInfo;

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
            ICommunication mostSuitableImplementation = CommunicationManager.GetMostSuitableImplementation();
            Stream stream = mostSuitableImplementation.ConnectToDevice(receiver);

            var sessionEncryption = new SessionEncryption();

            var transferRequest = new TransferRequest(
                Guid.NewGuid().ToString(),
                myDeviceInfo.DeviceId,
                myDeviceInfo.DeviceName,
                contentBase,
                sessionEncryption.GetMyPublicKey()
            );

            byte[] binaryTransferRequest = transferRequest.ToBinary();

            await stream.WriteAsync(binaryTransferRequest, cancellationToken);

            byte[] response = new byte[7];
            // ReSharper disable once MustUseReturnValue
            await stream.ReadAsync(response, cancellationToken);

            var responseAnswer = response.GetStringFromBytes().ToEnum<TransferRequestAnswers>();
            Logger.Info($"Received response answer: {responseAnswer}");

            if (responseAnswer == TransferRequestAnswers.Accept)
            {
                byte[] foreignPublicKey = new byte[67];
                // ReSharper disable once MustUseReturnValue
                await stream.ReadAsync(foreignPublicKey, cancellationToken);

                byte[] aesKey = sessionEncryption.CalculateAesKey(foreignPublicKey);
                byte[] iv = SessionEncryption.GenerateIvBytes();
                var ivBase64 = Convert.ToBase64String(iv).GetBytes().ToList();
                ivBase64.Add(0x00);

                await stream.WriteAsync(ivBase64.ToArray(), cancellationToken);

                await SessionEncryption.EncryptStream(stream, contentBase.DataStream!, aesKey, iv, progress, cancellationToken);

                Logger.Info("Done sending");
                return SendResponses.Success;
            }

            stream.Close();
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