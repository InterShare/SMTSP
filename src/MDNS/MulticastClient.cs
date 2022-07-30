using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace MDNS;

/// <summary>
///   Performs the magic to send and receive datagrams over multicast
///   sockets.
/// </summary>
internal sealed class MulticastClient : IDisposable
{
    /// <summary>
    ///   The port number assigned to Multicast DNS.
    /// </summary>
    /// <value>
    ///   Port number 5353.
    /// </value>
    public const int MulticastPort = 5353;

    private static readonly IPAddress _multicastAddressIp4 = IPAddress.Parse("224.0.0.251");
    private static readonly IPAddress _multicastAddressIp6 = IPAddress.Parse("FF02::FB");
    private static readonly IPEndPoint _mdnsEndpointIp6 = new(_multicastAddressIp6, MulticastPort);
    private static readonly IPEndPoint _mdnsEndpointIp4 = new(_multicastAddressIp4, MulticastPort);

    private readonly List<UdpClient> _receivers;
    private readonly ConcurrentDictionary<IPAddress, UdpClient> _senders = new();

    public event EventHandler<UdpReceiveResult>? MessageReceived;

    public MulticastClient(bool useIPv4, bool useIpv6, IEnumerable<NetworkInterface> nics)
    {
        // Setup the receivers.
        _receivers = new List<UdpClient>();

        UdpClient? receiver4 = null;
        if (useIPv4)
        {
            receiver4 = new UdpClient(AddressFamily.InterNetwork);
            receiver4.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
// #if NETSTANDARD2_0
//                 if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//                 {
//                     LinuxHelper.ReuseAddresss(receiver4.Client);
//                 }
// #endif
            receiver4.Client.Bind(new IPEndPoint(IPAddress.Any, MulticastPort));
            _receivers.Add(receiver4);
        }

        UdpClient? receiver6 = null;
        if (useIpv6)
        {
            receiver6 = new UdpClient(AddressFamily.InterNetworkV6);
            receiver6.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
// #if NETSTANDARD2_0
//                 if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//                 {
//                     LinuxHelper.ReuseAddresss(receiver6.Client);
//                 }
// #endif
            receiver6.Client.Bind(new IPEndPoint(IPAddress.IPv6Any, MulticastPort));
            _receivers.Add(receiver6);
        }

        // Get the IP addresses that we should send to.
        var addresses = nics
            .SelectMany(GetNetworkInterfaceLocalAddresses)
            .Where(a => (useIPv4 && a.AddressFamily == AddressFamily.InterNetwork)
                        || (useIpv6 && a.AddressFamily == AddressFamily.InterNetworkV6));
        foreach (IPAddress? address in addresses)
        {
            if (_senders.ContainsKey(address))
            {
                continue;
            }

            var localEndpoint = new IPEndPoint(address, MulticastPort);
            var sender = new UdpClient(address.AddressFamily);
            try
            {
                switch (address.AddressFamily)
                {
                    case AddressFamily.InterNetwork:
                        receiver4?.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(_multicastAddressIp4, address));
                        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
// #if NETSTANDARD2_0
//                             if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
//                             {
//                                 LinuxHelper.ReuseAddresss(sender.Client);
//                             }
// #endif
                        sender.Client.Bind(localEndpoint);
                        sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(_multicastAddressIp4));
                        sender.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, true);
                        break;
                    case AddressFamily.InterNetworkV6:
                        receiver6?.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(_multicastAddressIp6, address.ScopeId));
                        sender.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        sender.Client.Bind(localEndpoint);
                        sender.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, new IPv6MulticastOption(_multicastAddressIp6));
                        sender.Client.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, true);
                        break;
                    default:
                        throw new NotSupportedException($"Address family {address.AddressFamily}.");
                }

                if (!_senders.TryAdd(address, sender)) // Should not fail
                {
                    sender.Dispose();
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressNotAvailable)
            {
                // VPN NetworkInterfaces
                sender.Dispose();
            }
            catch (Exception e)
            {
                // Console.WriteLine($"Cannot setup send socket for {address}: {e.Message}");
                sender.Dispose();
            }
        }

        // Start listening for messages.
        foreach (UdpClient? r in _receivers)
        {
            Listen(r);
        }
    }

    public async Task SendAsync(byte[] message)
    {
        foreach (var sender in _senders)
        {
            try
            {
                IPEndPoint endpoint = sender.Key.AddressFamily == AddressFamily.InterNetwork ? _mdnsEndpointIp4 : _mdnsEndpointIp6;
                await sender.Value.SendAsync(
                        message, message.Length,
                        endpoint)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Console.WriteLine($"Sender {sender.Key} failure: {e.Message}");
                // eat it.
            }
        }
    }

    private void Listen(UdpClient receiver)
    {
        // ReceiveAsync does not support cancellation.  So the receiver is disposed
        // to stop it. See https://github.com/dotnet/corefx/issues/9848
        Task.Run(async () =>
        {
            try
            {
                var task = receiver.ReceiveAsync();

                _ = task.ContinueWith(_ => Listen(receiver), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously);

                _ = task.ContinueWith(x => MessageReceived?.Invoke(this, x.Result), TaskContinuationOptions.OnlyOnRanToCompletion | TaskContinuationOptions.RunContinuationsAsynchronously);

                await task.ConfigureAwait(false);
            }
            catch
            {
                // ignore
            }
        });
    }

    private static IEnumerable<IPAddress> GetNetworkInterfaceLocalAddresses(NetworkInterface nic)
    {
        return nic
                .GetIPProperties()
                .UnicastAddresses
                .Select(x => x.Address)
                .Where(x => x.AddressFamily != AddressFamily.InterNetworkV6 || x.IsIPv6LinkLocal);
    }

    #region IDisposable Support

    private bool _disposedValue; // To detect redundant calls

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                MessageReceived = null;

                foreach (UdpClient? receiver in _receivers)
                {
                    try
                    {
                        receiver.Dispose();
                    }
                    catch
                    {
                        // eat it.
                    }
                }
                _receivers.Clear();

                foreach (IPAddress? address in _senders.Keys)
                {
                    if (_senders.TryRemove(address, out UdpClient? sender))
                    {
                        try
                        {
                            sender.Dispose();
                        }
                        catch
                        {
                            // eat it.
                        }
                    }
                }
                _senders.Clear();
            }

            _disposedValue = true;
        }
    }

    ~MulticastClient()
    {
        Dispose(false);
    }

    // This code added to correctly implement the disposable pattern.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}