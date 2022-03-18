using System.Net;
using System.Net.Sockets;
using SMTSP.Core;
using SMTSP.Entities;
using SMTSP.Extensions;
using SMTSP.Helpers;

namespace SMTSP;

/// <summary>
/// Used to receive files.
/// </summary>
public class SmtspReceiver
{
    private const int DefaultPort = 42420;

    private bool _running;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private TcpListener? _tcpListener;
    private Thread? _listeningThread;
    private readonly object _listeningThreadLock = new object();

    private Func<TransferRequest, Task<bool>>? _onTransferRequestCallback;

    /// <summary>
    /// The port on which the receive server runs.
    /// </summary>
    public ushort Port { get; private set; }

    /// <summary>
    /// Invokes, when a new device is being discovered.
    /// </summary>
    public event EventHandler<SmtsFile> OnFileReceive = delegate { };

    private void ListenForConnections()
    {
        try
        {
            while (_running)
            {
                lock (_listeningThreadLock)
                {
                    if (!_running)
                    {
                        break;
                    }
                }

                if (_tcpListener == null)
                {
                    continue;
                }

                Logger.Info("Waiting for connections");

                TcpClient client = _tcpListener.AcceptTcpClient();
                Logger.Info("Connected!");

                NetworkStream stream = client.GetStream();

                GetMessageTypeResponse messageTypeResult = MessageTransformer.GetMessageType(stream);

                Logger.Info($"Received Request v{messageTypeResult.Version} with type {messageTypeResult.Type}");

                if (messageTypeResult.Type == MessageTypes.TransferRequest)
                {
                    var transferRequest = new TransferRequest();
                    transferRequest.FromStream(stream);

                    var result = false;

                    if (_onTransferRequestCallback != null)
                    {
                        lock (_listeningThreadLock)
                        {
                            result = _onTransferRequestCallback.Invoke(transferRequest).Result;
                        }
                    }

                    if (result)
                    {
                        byte[] resultInBytes = TransferRequestAnswers.Accept.ToLowerCamelCaseString().GetBytes()!.ToArray();
                        stream.Write(resultInBytes, 0, resultInBytes.Length);

                        var file = new SmtsFile
                        {
                            Name = transferRequest.FileName,
                            DataStream = stream,
                            FileSize = transferRequest.FileSize
                        };

                        OnFileReceive.Invoke(this, file);
                    }
                    else
                    {
                        byte[] resultInBytes = TransferRequestAnswers.Decline.ToString().GetBytes()!.ToArray();
                        stream.Write(resultInBytes, 0, resultInBytes.Length);

                        stream.Close();
                    }
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
    /// <returns>Returns a <c>bool</c> to indicate whether it started successful</returns>
    public bool StartReceiving()
    {
        try
        {
            Logger.Info("Trying to start tcp server...");

            try
            {
                Logger.Info($"Starting TCP Server with port {DefaultPort}");
                _tcpListener = new TcpListener(IPAddress.Any, DefaultPort);
                _tcpListener.Start();
            }
            catch (SocketException exception)
            {
                // TODO: handle this exception better than "Contains"
                if (exception.Message.ToLowerInvariant().Contains("already in use"))
                {
                    Logger.Info("Default port is already in use, choosing another one");
                    _tcpListener = new TcpListener(IPAddress.Any, 0);
                    _tcpListener.Start();
                }
                else
                {
                    Logger.Exception(exception);
                    _running = false;
                    return false;
                }
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
                _running = false;
                return false;
            }

            Port = ushort.Parse(((IPEndPoint)_tcpListener.LocalEndpoint).Port.ToString());

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _cancellationToken.Register(() =>
            {
                _tcpListener.Stop();
                _running = false;
                Logger.Success("Stopped tcp server");
            });

            Logger.Success($"Server is running on port: {Port}");
            _running = true;

            _listeningThread?.Interrupt();
            _listeningThread = new Thread(ListenForConnections)
            {
                IsBackground = true
            };
            _listeningThread.Start();
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
            _running = false;
        }

        return _running;
    }

    /// <summary>
    /// Stop looking for file requests
    /// </summary>
    public void StopReceiving()
    {
        _running = false;

        _listeningThread?.Join();
        _listeningThread?.Interrupt();

        if (_cancellationTokenSource != null && _cancellationToken.CanBeCanceled)
        {
            _cancellationTokenSource.Cancel();
        }
    }

    /// <summary>
    /// Incoming file requests will be invoked on callbacks registered through this method.
    /// It is necessary to at least register one callback or otherwise, no requests will be accepted.
    /// </summary>
    /// <param name="callbackFunction">Return <c>true</c> to accept the data transfer, <c>false</c> to decline it.</param>
    public void RegisterTransferRequestCallback(Func<TransferRequest, Task<bool>> callbackFunction)
    {
        lock (_listeningThreadLock)
        {
            _onTransferRequestCallback = callbackFunction;
        }
    }
}