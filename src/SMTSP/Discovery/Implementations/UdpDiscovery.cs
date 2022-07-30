using System.Collections.ObjectModel;
using System.Net;
using System.Net.Sockets;
using SMTSP.Core;
using SMTSP.Entities;
using SMTSP.Extensions;
using SMTSP.Helpers;
using Timer = System.Timers.Timer;

namespace SMTSP.Discovery.Implementations;

internal class UdpDiscovery : IDiscovery
{
    private readonly int[] _discoveryPorts = { 42400, 42410, 42420, 42430 };
    private readonly object _listeningThreadLock = new();

    private DeviceInfo _myDeviceInfo = null!;
    private bool _answerToLookupBroadcasts;
    private bool _receiving;
    private int _port;
    private Timer? _discoveringInterval;
    private Thread? _listeningThread;
    private UdpClient? _udpSocket;

    public ObservableCollection<DeviceInfo> DiscoveredDevices { get; } = new();

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

                    if (port == _discoveryPorts.Last())
                    {
                        return;
                    }
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

            Stream? stream = null;

            try
            {
                UdpReceiveResult receivedMessage = await _udpSocket.ReceiveAsync();
                stream = new MemoryStream(receivedMessage.Buffer);
                
                GetMessageTypeResponse? messageTypeResult = MessageTransformer.GetMessageType(stream);

                if (messageTypeResult == null)
                {
                    stream.Close();
                    await stream.DisposeAsync();
                    continue;
                }

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
                        string? deviceId = stream.GetProperty(nameof(DeviceInfo.DeviceId));

                        if (!string.IsNullOrEmpty(deviceId) && deviceId != _myDeviceInfo.DeviceId)
                        {
                            byte[] myDeviceAsBytes = _myDeviceInfo.ToBinary();
                            await _udpSocket.SendAsync(myDeviceAsBytes, myDeviceAsBytes.Length, receivedMessage.RemoteEndPoint.Address.ToString(), receivedMessage.RemoteEndPoint.Port);
                        }
                    }
                }
                else if (messageTypeResult.Type == MessageTypes.RemoveDeviceFromDiscovery)
                {
                    string? deviceId = stream.GetProperty(nameof(DeviceInfo.DeviceId));

                    if (!string.IsNullOrEmpty(deviceId))
                    {
                        RemoveDevice(deviceId, receivedMessage.RemoteEndPoint.Address.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
            }
            
            if (stream != null)
            {
                stream.Close();
                await stream.DisposeAsync();
            }
        }
    }

    private async void SendOutLookup(object? sender = null, System.Timers.ElapsedEventArgs? _ = null)
    {
        if (_udpSocket == null)
        {
            return;
        }

        Logger.Info("Sending out lookup signal");

        var messageInBytes = new List<byte>();
        messageInBytes.AddSmtsHeader(MessageTypes.DeviceLookupRequest);
        messageInBytes.AddProperty(nameof(_myDeviceInfo.DeviceId), _myDeviceInfo.DeviceId);

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
        
        StartReceiveLoop();

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
        messageInBytes.AddProperty(nameof(_myDeviceInfo.DeviceId), _myDeviceInfo.DeviceId);

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
        _discoveringInterval = new Timer();
        _discoveringInterval.Interval = 4000;
        _discoveringInterval.Elapsed += SendOutLookup;
        _discoveringInterval.AutoReset = true;
        _discoveringInterval.Enabled = true;
        
        _discoveringInterval?.Start();
    }

    public void StopDiscovering()
    {
        _discoveringInterval?.Stop();
        _discoveringInterval?.Dispose();
    }

    public void Dispose()
    {
        _discoveringInterval?.Dispose();
        
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