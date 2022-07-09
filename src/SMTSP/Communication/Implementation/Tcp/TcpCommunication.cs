using System.Net;
using System.Net.Sockets;
using SMTSP.Core;
using SMTSP.Entities;

namespace SMTSP.Communication.Implementation.Tcp;

internal class TcpCommunication : ICommunication
{
    private const int DefaultPort = 42420;
    private readonly DeviceInfo _myDevice;

    private bool _running;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private TcpListener? _tcpListener;
    private Thread? _listeningThread;

    public ushort Port { get; private set; }

    public TcpCommunication(DeviceInfo myDevice)
    {
        _myDevice = myDevice;
    }

    public event EventHandler<Stream> OnReceive = delegate {};

    public Task Start()
    {
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
            _myDevice.TcpPort = Port;

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

    public Stream ConnectToDevice(DeviceInfo receiver)
    {
        var client = new TcpClient(receiver.IpAddress, receiver.TcpPort);
        NetworkStream tcpStream = client.GetStream();

        return tcpStream;
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

                TcpClient client = _tcpListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

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