using System.Net;
using System.Net.Sockets;
using SMTSP.Core;
using SMTSP.Protocol.Discovery;

namespace SMTSP.Connection;

internal class TcpCommunication
{
    public static TcpCommunication Shared { get; } = new();

    private const int DefaultPort = 42420;
    private Device _myDevice = null!;

    private bool _running;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private TcpListener? _tcpListener;
    private Thread? _listeningThread;

    public ushort Port { get; private set; }

    public event EventHandler<Stream> OnReceive = delegate {};

    public Task Start(Device myDevice)
    {
        _myDevice = myDevice;

        try
        {
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
                    return Task.CompletedTask;
                }
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
                _running = false;
                return Task.CompletedTask;
            }

            Port = ushort.Parse(((IPEndPoint)_tcpListener.LocalEndpoint).Port.ToString());
            _myDevice.TcpConnectionInfo.Port = Port;

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

        return Task.CompletedTask;
    }

    public Stream ConnectToDevice(Device receiver)
    {
        var ipAddress = IPAddress.Parse(receiver.TcpConnectionInfo.IpAddress);
        var client = new TcpClient(ipAddress.AddressFamily);
        client.Connect(ipAddress, Convert.ToInt32(receiver.TcpConnectionInfo.Port));

        return client.GetStream();
    }

    public void Dispose()
    {
        _running = false;

        _listeningThread?.Join();
        _listeningThread?.Interrupt();

        if (_cancellationTokenSource != null && _cancellationToken.CanBeCanceled)
        {
            _cancellationTokenSource.Cancel();
        }

        _tcpListener?.Stop();
    }

    private void ListenForConnections()
    {
        try
        {
            while (_running)
            {
                if (_tcpListener == null)
                {
                    continue;
                }

                var client = _tcpListener.AcceptTcpClient();
                var stream = client.GetStream();

                OnReceive.Invoke(this, stream);
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
}
