using System.Collections.ObjectModel;
using SMTSP.Entities;

namespace SMTSP.Discovery;

/// <summary>
/// Used to discover devices in the current network.
/// </summary>
public class Discovery : IDisposable
{
    private readonly IDiscovery _discovery;

    /// <summary>
    /// Holds the list of discovered devices.
    /// </summary>
    public readonly ObservableCollection<DeviceInfo> DiscoveredDevices;

    /// <param name="myDevice">The current device, used to advertise on the network, so that other devices can find this one</param>
    /// <param name="discoveryType">Select which system should be used for discovery</param>
    public Discovery(DeviceInfo myDevice, DiscoveryTypes discoveryType = DiscoveryTypes.UdpBroadcasts)
    {
        _discovery = discoveryType == DiscoveryTypes.Mdns ? new MdnsDiscovery() : UdpDiscoveryAndAdvertiser.Instance;
        _discovery.SetMyDevice(myDevice);
        DiscoveredDevices = _discovery.DiscoveredDevices;
    }


    /// <summary>
    /// Sends a signal, to queries all devices found on the network.
    /// </summary>
    public void SendOutLookupSignal()
    {
        _discovery.SendOutLookupSignal();
    }

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    public void Dispose()
    {
        _discovery.Dispose();
    }
}