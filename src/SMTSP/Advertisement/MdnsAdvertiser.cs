using System.Net.NetworkInformation;
using MDNS;
using SMTSP.Core;
using SMTSP.Entities;

namespace SMTSP.Advertisement;

internal class MdnsAdvertiser : IAdvertiser
{
    private DeviceInfo _myDevice = null!;
    private ServiceDiscovery _serviceDiscovery = null!;

    public void SetMyDevice(DeviceInfo myDevice)
    {
        _myDevice = myDevice;
        _serviceDiscovery = new ServiceDiscovery();

        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        Advertise();
    }

    /// <summary>
    /// Starts advertising the current device.
    /// </summary>
    public void Advertise()
    {
        var serviceProfile = new ServiceProfile(_myDevice.DeviceId, SmtsConfiguration.ServiceName, _myDevice.TcpPort);
        serviceProfile.AddProperty("deviceId", _myDevice.DeviceId);
        serviceProfile.AddProperty("deviceName", _myDevice.DeviceName);
        serviceProfile.AddProperty("type", _myDevice.DeviceType);
        serviceProfile.AddProperty("smtspVersion", SmtsConfiguration.ProtocolVersion.ToString());
        serviceProfile.AddProperty("port", _myDevice.TcpPort.ToString());
        serviceProfile.AddProperty("capabilities", string.Join(", ", _myDevice.Capabilities));

        _serviceDiscovery.Advertise(serviceProfile);
        _serviceDiscovery.Announce(serviceProfile);
    }

    /// <summary>
    /// Stops advertising the current device.
    /// </summary>
    public void StopAdvertising()
    {
        var serviceProfile = new ServiceProfile(_myDevice.DeviceId, SmtsConfiguration.ServiceName, _myDevice.TcpPort);
        _serviceDiscovery.Unadvertise(serviceProfile);
    }

    public void Dispose()
    {
        _serviceDiscovery.Dispose();
    }
}