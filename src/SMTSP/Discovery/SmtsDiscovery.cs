using System.Net;
using System.Net.Sockets;
using SMTSP.Core;
using SMTSP.Discovery.Entities;
using SMTSP.Entities;
using SMTSP.Extensions;
using SMTSP.Helpers;

namespace SMTSP.Discovery;

public class SmtsDiscovery
{
    private readonly int[] _discoveryPorts = { 4240, 4241, 4242 };
    private readonly object _listeningThreadLock = new object();
    private readonly DiscoveryDeviceInfo _myDeviceInfo;

    private bool _answerToLookupBroadcasts = false;
    private Timer? _discoveringInterval;
    private bool _receiving;
    private int _port;
    private UdpClient? _udpSocket;
    private Thread? _listeningThread;

    public readonly List<DiscoveryDeviceInfo> DiscoveredDevices = new List<DiscoveryDeviceInfo>();
    public event EventHandler<DiscoveryDeviceInfo> OnNewDeviceDiscovered = delegate { };


    public SmtsDiscovery(DiscoveryDeviceInfo myDevice)
    {
        _myDeviceInfo = myDevice;

        SetupUdpSocket();
    }

    private void SetupUdpSocket()
    {
        try
        {
            foreach (int port in _discoveryPorts)
            {
                try
                {
                    _port = port;
                    _udpSocket = new UdpClient(_port);
                    break;
                }
                catch (Exception exception)
                {
                    Logger.Exception(exception);
                }
            }

            _myDeviceInfo.DiscoveryPort = _port;

            Logger.Info($"Device Discoverer running at port: {_port}");
        }
        catch (SocketException exception)
        {
            Logger.Error($"Error while trying to enable UDP Socket {exception}");
        }
    }

    private void AddNewDevice(DiscoveryDeviceInfo deviceInfo)
    {
        if (deviceInfo.TransferPort == -1)
        {
            return;
        }

        lock (DiscoveredDevices)
        {
            DiscoveryDeviceInfo existingDeviceInfo = DiscoveredDevices.Find(element =>
                element.DeviceId == deviceInfo.DeviceId &&
                element.IpAddress == deviceInfo.IpAddress &&
                element.DiscoveryPort == deviceInfo.DiscoveryPort);

            if (existingDeviceInfo == null)
            {
                DiscoveredDevices.Add(deviceInfo);

                OnNewDeviceDiscovered.Invoke(this, deviceInfo);
            }
        }
    }

    private async void Receive()
    {
        try
        {
            bool answerToLookupBroadcasts;

            while (_receiving)
            {
                lock (_listeningThreadLock)
                {
                    if (!_receiving)
                    {
                        break;
                    }

                    answerToLookupBroadcasts = _answerToLookupBroadcasts;
                }

                if (_udpSocket == null)
                {
                    continue;
                }

                try
                {
                    UdpReceiveResult receivedMessage = await _udpSocket.ReceiveAsync();
                    using var stream = new MemoryStream(receivedMessage.Buffer);
                    GetMessageTypeResponse messageTypeResult = MessageTransformer.GetMessageType(stream);

                    if (messageTypeResult.Type == MessageTypes.DeviceInfo)
                    {
                        var receivedDevice = new DiscoveryDeviceInfo();
                        receivedDevice.FromStream(stream);
                        receivedDevice.IpAddress = receivedMessage.RemoteEndPoint.Address.ToString();

                        if (receivedDevice.DeviceId != _myDeviceInfo.DeviceId)
                        {
                            AddNewDevice(receivedDevice);
                        }
                    }
                    else if (messageTypeResult.Type == MessageTypes.DeviceLookupRequest && answerToLookupBroadcasts)
                    {
                        string deviceId = stream.GetStringTillEndByte(0x00);

                        if (!string.IsNullOrEmpty(deviceId) && deviceId != _myDeviceInfo.DeviceId)
                        {
                            byte[] myDeviceAsBytes = _myDeviceInfo.ToBinary();

                            try
                            {
                                await _udpSocket.SendAsync(myDeviceAsBytes, myDeviceAsBytes.Length, receivedMessage.RemoteEndPoint.Address.ToString(), receivedMessage.RemoteEndPoint.Port);
                            }
                            catch (Exception exception)
                            {
                                Logger.Exception(exception);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                    // Error while trying to read message. Do nothing and move on to the next
                }
            }
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
        }
        finally
        {
            Logger.Info("Closing udp socket");
            _udpSocket?.Close();
        }
    }

    private async void SendOutLookup(object? sender = null)
    {
        if (_udpSocket == null)
        {
            return;
        }

        Logger.Info("Sending out lookup signal");

        foreach (int port in _discoveryPorts)
        {
            var messageInBytes = new List<byte>();

            messageInBytes.AddSmtsHeader(MessageTypes.DeviceLookupRequest);
            messageInBytes.AddRange(_myDeviceInfo.DeviceId.GetBytes()!);
            messageInBytes.Add(0x00);

            try
            {
                await _udpSocket.SendAsync(messageInBytes.ToArray(), messageInBytes.Count, IPAddress.Broadcast.ToString(), port);
            }
            catch(Exception exception)
            {
                Logger.Exception(exception);
            }
        }
    }

    /// <summary>
    /// Broadcast my device in the network once
    /// </summary>
    public async void BroadcastMyDevice()
    {
        if (_udpSocket == null)
        {
            return;
        }

        Logger.Info("Broadcasting my device");

        foreach (int port in _discoveryPorts)
        {
            byte[] broadcastMessageInBytes = _myDeviceInfo.ToBinary();

            try
            {
                await _udpSocket.SendAsync(broadcastMessageInBytes.ToArray(), broadcastMessageInBytes.Length, IPAddress.Broadcast.ToString(), port);
            }
            catch(Exception exception)
            {
                Logger.Exception(exception);
            }
        }
    }

    /// <summary>
    /// Enables this device to be found by other devices in the same network.
    /// </summary>
    public void AllowToBeDiscovered()
    {
        _answerToLookupBroadcasts = true;

        if (!_receiving)
        {
            _listeningThread?.Interrupt();
            _listeningThread = new Thread(Receive)
            {
                IsBackground = true
            };

            _listeningThread.Start();
            _receiving = true;
        }

        BroadcastMyDevice();
    }

    /// <summary>
    /// No one will be able to discover this device for SMTSP transfers.
    /// </summary>
    public void DisallowToBeDiscovered()
    {
        _answerToLookupBroadcasts = false;
    }

    /// <summary>
    /// Start searching for available devices.
    /// This sends out a lookup request every 5 seconds till stopped with the <c>StopDiscovering()</c> method.
    /// </summary>
    public void StartDiscovering()
    {
        lock (DiscoveredDevices)
        {
            DiscoveredDevices.Clear();
        }

        if (_discoveringInterval == null)
        {
            if (!_receiving)
            {
                _listeningThread?.Interrupt();
                _listeningThread = new Thread(Receive)
                {
                    IsBackground = true
                };

                _listeningThread.Start();
                _receiving = true;
            }

            SendOutLookup();

            _discoveringInterval = new Timer(SendOutLookup, null, 5000, 5000);
        }
    }

    /// <summary>
    /// Stop sending out lookup requests every 5 seconds
    /// </summary>
    public void StopDiscovering()
    {
        _receiving = false;
        _listeningThread?.Join();
        _listeningThread?.Interrupt();
        _discoveringInterval?.Dispose();
        _discoveringInterval = null;
    }
}