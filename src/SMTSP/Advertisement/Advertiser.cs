using SMTSP.Discovery;
using SMTSP.Entities;

namespace SMTSP.Advertisement;

/// <summary>
///
/// </summary>
public class Advertiser : IDisposable
{
    private readonly IAdvertiser _advertiser;
    private readonly DiscoveryTypes _discoveryTypes;

    /// <param name="myDevice"></param>
    /// <param name="discoveryType">Select which system should be used for discovery</param>
    public Advertiser(DeviceInfo myDevice, DiscoveryTypes discoveryType = DiscoveryTypes.UdpBroadcasts)
    {
        _discoveryTypes = discoveryType;
        _advertiser = discoveryType == DiscoveryTypes.Mdns ? new MdnsAdvertiser() : UdpDiscoveryAndAdvertiser.Instance;
        _advertiser.SetMyDevice(myDevice);
    }

    /// <summary>
    /// Starts advertising the current device.
    /// </summary>
    public void Advertise()
    {
        _advertiser.Advertise();
    }

    /// <summary>
    /// Stops advertising the current device.
    /// </summary>
    public void StopAdvertising()
    {
        _advertiser.StopAdvertising();
    }

    /// <summary>
    /// Dispose everything.
    /// </summary>
    public void Dispose()
    {

        if (_discoveryTypes == DiscoveryTypes.UdpBroadcasts)
        {
            (_advertiser as UdpDiscoveryAndAdvertiser)?.DisposeAdvertiser();
            return;
        }

        _advertiser.Dispose();
    }
}