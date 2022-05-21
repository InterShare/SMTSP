using SMTSP.Discovery;
using SMTSP.Entities;

namespace SMTSP.Advertisement;

/// <summary>
///
/// </summary>
public class Advertiser : IDisposable
{
    private readonly List<IAdvertiser> _advertiserImplementations = new();

    /// <param name="myDevice"></param>
    public Advertiser(DeviceInfo myDevice)
    {
        _advertiserImplementations.Add(new MdnsAdvertiser());
        _advertiserImplementations.Add(UdpDiscoveryAndAdvertiser.Instance);

        foreach (IAdvertiser advertiser in _advertiserImplementations)
        {
            advertiser.SetMyDevice(myDevice);
        }
    }

    /// <summary>
    /// Starts advertising the current device.
    /// </summary>
    public void Advertise()
    {
        foreach (IAdvertiser advertiserImplementation in _advertiserImplementations)
        {
            advertiserImplementation.Advertise();
        }
    }

    /// <summary>
    /// Stops advertising the current device.
    /// </summary>
    public void StopAdvertising()
    {
        foreach (IAdvertiser advertiserImplementation in _advertiserImplementations)
        {
            advertiserImplementation.StopAdvertising();
        }
    }

    /// <summary>
    /// Dispose everything.
    /// </summary>
    public void Dispose()
    {
        foreach (IAdvertiser advertiser in _advertiserImplementations)
        {
            if (advertiser.GetType() == typeof(UdpDiscoveryAndAdvertiser))
            {
                (advertiser as UdpDiscoveryAndAdvertiser)?.DisposeDiscovery();
            }
            else
            {
                advertiser.Dispose();
            }
        }
    }
}