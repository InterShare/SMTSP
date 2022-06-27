using System.Collections.ObjectModel;
using System.Collections.Specialized;
using SMTSP.Entities;

namespace SMTSP.Discovery;

/// <summary>
/// Used to discover devices in the current network.
/// </summary>
public class Discovery : IDisposable
{
    private readonly List<IDiscovery> _discoveryImplementations = new List<IDiscovery>();

    /// <summary>
    /// Holds the list of discovered devices.
    /// </summary>
    public readonly ObservableCollection<DeviceInfo> DiscoveredDevices = new();

    /// <param name="myDevice">The current device, used to advertise on the network, so that other devices can find this one</param>
    public Discovery(DeviceInfo myDevice)
    {
        _discoveryImplementations.Add(new MdnsDiscovery());
        _discoveryImplementations.Add(UdpDiscoveryAndAdvertiser.Instance);

        foreach (IDiscovery discovery in _discoveryImplementations)
        {
            discovery.SetMyDevice(myDevice);

            lock (DiscoveredDevices)
            {
                foreach (DeviceInfo discoveredDevice in discovery.DiscoveredDevices)
                {
                    if (DiscoveredDevices.FirstOrDefault(device => device.DeviceId == discoveredDevice.DeviceId) == null)
                    {
                        DiscoveredDevices.Add(discoveredDevice);
                    }
                }
            }

            discovery.DiscoveredDevices.CollectionChanged += DiscoveredDevicesOnCollectionChanged;
        }
    }

    private void DiscoveredDevicesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        lock (DiscoveredDevices)
        {
            if (e.NewItems != null)
            {
                foreach (DeviceInfo newDeviceInfo in e.NewItems)
                {
                    DeviceInfo? existingDevice = DiscoveredDevices.FirstOrDefault(device => device.DeviceId == newDeviceInfo.DeviceId);

                    if (existingDevice == null)
                    {
                        DiscoveredDevices.Add(newDeviceInfo);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (DeviceInfo newDeviceInfo in e.OldItems)
                {
                    DeviceInfo? existingDevice = DiscoveredDevices.FirstOrDefault(device => device.DeviceId == newDeviceInfo.DeviceId);

                    if (existingDevice != null)
                    {
                        DiscoveredDevices.Remove(existingDevice);
                    }
                }
            }
        }
    }


    /// <summary>
    /// Sends a signal, to queries all devices found on the network.
    /// </summary>
    public void StartDiscovering()
    {
        foreach (IDiscovery discovery in _discoveryImplementations)
        {
            discovery.StartDiscovering();
        }
    }

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (IDiscovery discoveryImplementation in _discoveryImplementations)
        {
            if (discoveryImplementation.GetType() == typeof(UdpDiscoveryAndAdvertiser))
            {
                (discoveryImplementation as UdpDiscoveryAndAdvertiser)?.DisposeDiscovery();
            }
            else
            {
                discoveryImplementation?.Dispose();
            }
        }
    }
}