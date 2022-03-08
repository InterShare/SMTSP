using System.Net.NetworkInformation;
using MDNS;
using SMTSP.Core;
using SMTSP.Entities;

namespace SMTSP.Discovery;

/// <summary>
///
/// </summary>
public class Advertiser : IDisposable
{
    private readonly DeviceInfo _myDevice;
    private readonly ServiceDiscovery _serviceDiscovery;

    /// <param name="myDevice"></param>
    public Advertiser(DeviceInfo myDevice)
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
        var serviceProfile = new ServiceProfile(_myDevice.DeviceId, SmtsConfig.ServiceName, _myDevice.Port);
        serviceProfile.AddProperty("deviceId", _myDevice.DeviceId);
        serviceProfile.AddProperty("deviceName", _myDevice.DeviceName);
        serviceProfile.AddProperty("type", _myDevice.DeviceType);
        serviceProfile.AddProperty("smtspVersion", SmtsConfig.ProtocolVersion.ToString());
        serviceProfile.AddProperty("port", _myDevice.Port.ToString());

        _serviceDiscovery.Advertise(serviceProfile);
        _serviceDiscovery.Announce(serviceProfile);
    }

    /// <summary>
    /// Stops advertising the current device.
    /// </summary>
    public void StopAdvertising()
    {
        var serviceProfile = new ServiceProfile(_myDevice.DeviceId, SmtsConfig.ServiceName, _myDevice.Port);
        _serviceDiscovery.Unadvertise(serviceProfile);
    }

    public void Dispose()
    {
        _serviceDiscovery.Dispose();
    }
}