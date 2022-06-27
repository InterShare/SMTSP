using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using SMTSP.Advertisement;
using SMTSP.Core;
using SMTSP.Entities;
using SMTSP.Extensions;
using SMTSP.Helpers;

namespace SMTSP.Discovery;

/*
 * To reduce CPU load only a single UDP Client is opened and used for both; discovery and advertisement.
 */

internal class UdpDiscoveryAndAdvertiser : IDiscovery, IAdvertiser
{
    private static UdpDiscoveryAndAdvertiser? _instance;

    private readonly int[] _discoveryPorts = { 42400, 42410, 42420 };
    private readonly object _listeningThreadLock = new object();

    private DeviceInfo _myDeviceInfo = null!;
    private bool _answerToLookupBroadcasts;
    private bool _receiving;
    private int _port;
    private Timer? _discoveringInterval;
    private Thread? _listeningThread;
    private UdpClient? _udpSocket;
    private bool _discoveryDisposed;
    private bool _advertisingDisposed;

    public static UdpDiscoveryAndAdvertiser Instance => _instance ??= new UdpDiscoveryAndAdvertiser();

    public ObservableCollection<DeviceInfo> DiscoveredDevices { get; } = new ObservableCollection<DeviceInfo>();

    private void SetupUdpSocket()
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
                if (exception.Message.Contains("Address already in use"))
                {
                    Logger.Info($"Address already in use: {port}");
                }
                else
                {
                    Logger.Exception(exception);
                }
            }
        }

        Logger.Info($"Device Discoverer running at port: {_port}");
    }

    private void AddNewDevice(DeviceInfo deviceInfo)
    {
        lock (DiscoveredDevices)
        {
            DeviceInfo? existingDeviceInfo = DiscoveredDevices.FirstOrDefault(element =>
                element.DeviceId == deviceInfo.DeviceId &&
                element.IpAddress == deviceInfo.IpAddress);

            if (existingDeviceInfo == null)
            {
                DiscoveredDevices.Add(deviceInfo);
            }
        }
    }

    private void RemoveDevice(string deviceId, string ipAddress)
    {
        lock (DiscoveredDevices)
        {
            DeviceInfo? existingDeviceInfo = DiscoveredDevices.FirstOrDefault(element =>
                element.DeviceId == deviceId &&
                element.IpAddress == ipAddress);

            if (existingDeviceInfo != null)
            {
                DiscoveredDevices.Remove(existingDeviceInfo);
            }
        }
    }

    private async void Receive()
    {
        while (_receiving)
        {
            try
            {
                bool answerToLookupBroadcasts;

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

                UdpReceiveResult receivedMessage = await _udpSocket.ReceiveAsync();
                using var stream = new MemoryStream(receivedMessage.Buffer);
                GetMessageTypeResponse messageTypeResult = MessageTransformer.GetMessageType(stream);

                // ReSharper disable once ConvertIfStatementToSwitchStatement
                if (messageTypeResult.Type == MessageTypes.DeviceInfo)
                {
                    var receivedDevice = new DeviceInfo(stream)
                    {
                        IpAddress = receivedMessage.RemoteEndPoint.Address.ToString()
                    };

                    if (receivedDevice.DeviceId != _myDeviceInfo.DeviceId)
                    {
                        AddNewDevice(receivedDevice);
                    }
                }
                else if (messageTypeResult.Type == MessageTypes.DeviceLookupRequest)
                {
                    if (answerToLookupBroadcasts)
                    {
                        string deviceId = stream.GetStringTillEndByte(0x00);

                        if (!string.IsNullOrEmpty(deviceId) && deviceId != _myDeviceInfo.DeviceId)
                        {
                            byte[] myDeviceAsBytes = _myDeviceInfo.ToBinary();
                            await _udpSocket.SendAsync(myDeviceAsBytes, myDeviceAsBytes.Length, receivedMessage.RemoteEndPoint.Address.ToString(), receivedMessage.RemoteEndPoint.Port);
                        }
                    }
                }
                else if (messageTypeResult.Type == MessageTypes.RemoveDeviceFromDiscovery)
                {
                    string deviceId = stream.GetStringTillEndByte(0x00);

                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        RemoveDevice(deviceId, receivedMessage.RemoteEndPoint.Address.ToString());
                    }
                }

                stream.Close();
                await stream.DisposeAsync();
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
            }
        }
    }

    private async void SendOutLookup(object? sender = null)
    {
        if (_udpSocket == null)
        {
            return;
        }

        Logger.Info("Sending out lookup signal");

        var messageInBytes = new List<byte>();
        messageInBytes.AddSmtsHeader(MessageTypes.DeviceLookupRequest);
        messageInBytes.AddRange(_myDeviceInfo.DeviceId.GetBytes());
        messageInBytes.Add(0x00);

        foreach (int port in _discoveryPorts)
        {
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

    private void StartReceiveLoop()
    {
        if (!_receiving)
        {
            _receiving = true;
            _listeningThread?.Interrupt();
            _listeningThread = new Thread(Receive)
            {
                IsBackground = true
            };

            _listeningThread.Start();
        }
    }

    public void SetMyDevice(DeviceInfo myDevice)
    {
        _myDeviceInfo = myDevice;

        SetupUdpSocket();
        StartReceiveLoop();
    }

    public void Advertise()
    {
        if (_udpSocket == null)
        {
            return;
        }

        _answerToLookupBroadcasts = true;
        StartReceiveLoop();

        Logger.Info("Broadcasting my device");

        foreach (int port in _discoveryPorts)
        {
            byte[] broadcastMessageInBytes = _myDeviceInfo.ToBinary();

            try
            {
                _udpSocket.SendAsync(broadcastMessageInBytes.ToArray(), broadcastMessageInBytes.Length, IPAddress.Broadcast.ToString(), port);
            }
            catch(Exception exception)
            {
                Logger.Exception(exception);
            }
        }
    }

    public void StopAdvertising()
    {
        if (_udpSocket == null)
        {
            return;
        }

        _answerToLookupBroadcasts = false;

        var messageInBytes = new List<byte>();
        messageInBytes.AddSmtsHeader(MessageTypes.RemoveDeviceFromDiscovery);
        messageInBytes.AddRange(_myDeviceInfo.DeviceId.GetBytes());
        messageInBytes.Add(0x00);

        foreach (int port in _discoveryPorts)
        {
            try
            {
                if (_udpSocket != null)
                {
                    lock (_udpSocket)
                    {
                        _udpSocket?.Send(messageInBytes.ToArray(), messageInBytes.Count, IPAddress.Broadcast.ToString(), port);
                    }
                }
            }
            catch(Exception exception)
            {
                Logger.Exception(exception);
            }
        }
    }

    public void StartDiscovering()
    {
        _discoveringInterval = new Timer(SendOutLookup, new AutoResetEvent(true), 0, 4000);
    }

    public void DisposeDiscovery()
    {
        _discoveryDisposed = true;
        _discoveringInterval?.Dispose();

        Dispose();
    }

    public void DisposeAdvertiser()
    {
        _advertisingDisposed = true;
        _answerToLookupBroadcasts = false;

        Dispose();
    }

    public void Dispose()
    {
        if (_advertisingDisposed && _discoveryDisposed)
        {
            lock (_listeningThreadLock)
            {
                _receiving = false;
            }

            if (_udpSocket != null)
            {
                lock (_udpSocket)
                {
                    _udpSocket?.Close();
                    _udpSocket?.Dispose();
                    _udpSocket = null;
                }
            }
        }
    }
}