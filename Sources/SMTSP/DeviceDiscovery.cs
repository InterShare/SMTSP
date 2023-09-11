using System.Collections.ObjectModel;
using SMTSP.Discovery;
using SMTSP.Protocol.Discovery;

namespace SMTSP;

public class DeviceDiscovery
{
    private readonly BonjourDiscovery _innerDiscoveryService;

    public ObservableCollection<Device> DiscoveredDevices { get; }

    public DeviceDiscovery(Device myDevice)
    {
        _innerDiscoveryService = new BonjourDiscovery(myDevice);
        DiscoveredDevices = _innerDiscoveryService.DiscoveredDevices;
    }

    public void Register(ushort port)
    {
        _innerDiscoveryService.Register(port);
    }

    public void Unregister()
    {
        _innerDiscoveryService.Unregister();
    }

    public void Browse()
    {
        _innerDiscoveryService.Browse();
    }
}
