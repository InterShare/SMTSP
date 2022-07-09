using SMTSP.Communication;
using SMTSP.Communication.Implementation.Tcp;
using SMTSP.Core;
using SMTSP.Encryption;
using SMTSP.Entities;
using SMTSP.Entities.Content;
using SMTSP.Extensions;
using SMTSP.Helpers;

namespace SMTSP;

/// <summary>
/// Used to receive data.
/// </summary>
public class SmtspReceiver : IDisposable
{
    private Func<TransferRequest, Task<bool>>? _onTransferRequestCallback;

    /// <summary>
    /// Holds a list of all implementations used for a file transfer.
    /// </summary>
    public readonly List<ICommunication> CommunicationImplementations = new List<ICommunication>();


    /// <param name="myDevice">The DeviceInfo of this device.</param>
    public SmtspReceiver(DeviceInfo myDevice)
    {
        CommunicationImplementations.Add(new TcpCommunication(myDevice));
    }

    /// <summary>
    /// Invokes, when a new device is being discovered.
    /// </summary>
    public event EventHandler<SmtspContentBase> OnContentReceive = delegate { };

    private void OnReceive(object? sender, Stream? stream)
    {
        try
        {
            GetMessageTypeResponse messageTypeResult = MessageTransformer.GetMessageType(stream);

            Logger.Info($"Received Request v{messageTypeResult.Version} with type {messageTypeResult.Type}");

            if (messageTypeResult.Type == MessageTypes.TransferRequest)
            {
                var transferRequest = new TransferRequest(stream);

                bool result = false;

                if (_onTransferRequestCallback != null)
                {
                    result = _onTransferRequestCallback.Invoke(transferRequest).Result;
                }

                if (result)
                {
                    var encryption = new SessionEncryption();
                    byte[] publicKey = encryption.GetMyPublicKey();

                    byte[] resultInBytes = TransferRequestAnswers.Accept.ToLowerCamelCaseString().GetBytes().ToArray();
                    stream.Write(resultInBytes, 0, resultInBytes.Length);

                    stream.Write(publicKey, 0, publicKey.Length);

                    byte[] iv = Convert.FromBase64String(stream.GetStringTillEndByte(0x00));
                    byte[] aesKey = encryption.CalculateAesKey(transferRequest.SessionPublicKey!);

                    Stream decrypted = SessionEncryption.CreateDecryptedStream(stream, aesKey, iv);

                    transferRequest.ContentBase.DataStream = decrypted;

                    OnContentReceive.Invoke(this, transferRequest.ContentBase);
                }
                else
                {
                    byte[] resultInBytes = TransferRequestAnswers.Decline.ToString().GetBytes().ToArray();
                    stream.Write(resultInBytes, 0, resultInBytes.Length);

                    stream.Close();
                }
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Info("Canceled Operation");
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
        }
    }

    /// <summary>
    /// Begin looking for data transfer requests.
    /// </summary>
    /// <returns>Returns a <c>bool</c> to indicate whether the start succeeded.</returns>
    public async Task StartReceiving()
    {
        try
        {
            foreach (ICommunication communicationService in CommunicationImplementations)
            {
                await communicationService.Start();
                communicationService.OnReceive += OnReceive;
            }
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
        }
    }

    /// <summary>
    /// Stop looking for requests and dispose all services.
    /// </summary>
    public void Dispose()
    {
        foreach (ICommunication communicationService in CommunicationImplementations)
        {
            communicationService.Dispose();
        }
    }

    /// <summary>
    /// Incoming requests will be invoked on callbacks registered through this method.
    /// It is necessary to at least register one callback or otherwise, no requests will be accepted.
    /// </summary>
    /// <param name="callbackFunction">Return <c>true</c> to accept the data transfer, <c>false</c> to decline it.</param>
    public void RegisterTransferRequestCallback(Func<TransferRequest, Task<bool>> callbackFunction)
    {
        _onTransferRequestCallback = callbackFunction;
    }
}