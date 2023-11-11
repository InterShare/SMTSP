using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using SMTSP.Core;
using SMTSP.Discovery;

namespace SMTSP.Communication.Backends;

internal class TcpCommunicationBackend : ICommunicationBackend
{
    public static TcpCommunicationBackend Shared { get; } = new();

    private const int DefaultPort = 80;
    private Device _myDevice = null!;
    private X509Certificate2 _certificate = null!;

    private bool _running;
    private CancellationTokenSource? _cancellationTokenSource;
    private CancellationToken _cancellationToken;
    private TcpListener? _tcpListener;
    private Thread? _listeningThread;
    private ushort _port;


    public event EventHandler<SslStream> OnReceive = delegate {};

    public Task Start(Device myDevice, X509Certificate2 certificate)
    {
        _myDevice = myDevice;
        _certificate = certificate;

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

            _port = ushort.Parse(((IPEndPoint)_tcpListener.LocalEndpoint).Port.ToString());
            _myDevice.TcpConnectionInfo.Port = _port;

            _cancellationTokenSource = new CancellationTokenSource();
            _cancellationToken = _cancellationTokenSource.Token;
            _cancellationToken.Register(() =>
            {
                _tcpListener.Stop();
                _running = false;
                Logger.Success("Stopped tcp server");
            });

            Logger.Success($"Server is running on port: {_port}");
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

    public (SslStream, IDisposable) ConnectToDevice(Device receiver)
    {
        var ipAddress = Dns.GetHostEntry(receiver.TcpConnectionInfo.IpAddress)
            .AddressList
            .First(addr => addr.AddressFamily == AddressFamily.InterNetwork);

        var client = new TcpClient(ipAddress.AddressFamily);
        client.Connect(ipAddress, Convert.ToInt32(receiver.TcpConnectionInfo.Port));

        var sslStream = new SslStream(
            client.GetStream(),
            false,
            ValidateCertificate,
            null
        );

        sslStream.AuthenticateAsClient(
            targetHost: receiver.TcpConnectionInfo.IpAddress,
            clientCertificates: new X509CertificateCollection { _certificate },
            checkCertificateRevocation: true
        );

        if (!sslStream.IsEncrypted || !sslStream.IsAuthenticated || !sslStream.IsMutuallyAuthenticated)
        {
            throw new AuthenticationException($"Error. Stream is either not encrypted, or not authenticated.\nEncrypted: {sslStream.IsEncrypted}\nAuthenticated: {sslStream.IsAuthenticated}");
        }

        return (sslStream, client);
    }

    private static bool ValidateCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == SslPolicyErrors.None)
        {
            return true;
        }

        if (sslPolicyErrors is (SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors)
            or SslPolicyErrors.RemoteCertificateChainErrors)
        {
            return true;
        }

        Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

        return false;
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

                var sslStream = new SslStream(
                    client.GetStream(),
                    false,
                    ValidateCertificate,
                    null
                );

                sslStream.AuthenticateAsServer(_certificate, clientCertificateRequired: true, checkCertificateRevocation: true);

                if (!sslStream.IsEncrypted || !sslStream.IsAuthenticated || !sslStream.IsMutuallyAuthenticated)
                {
                    throw new AuthenticationException($"Error. Stream is either not encrypted, or not authenticated.\nEncrypted: {sslStream.IsEncrypted}\nAuthenticated: {sslStream.IsAuthenticated}");
                }

                OnReceive.Invoke(this, sslStream);
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
