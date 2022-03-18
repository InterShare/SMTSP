namespace SMTSP.Entities;

/// <summary>
/// Available systems that can be used to discover devices in a network.
/// </summary>
public enum DiscoveryTypes
{
    /// <summary>
    /// Uses UDP broadcasts to discover devices.
    /// </summary>
    UdpBroadcasts,

    /// <summary>
    /// Uses MDNS Service Discovery to discover devices.
    /// </summary>
    Mdns
}