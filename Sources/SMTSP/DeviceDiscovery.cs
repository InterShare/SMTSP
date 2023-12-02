using System.Collections.ObjectModel;
using SMTSP.Discovery;

namespace SMTSP;

public class DeviceDiscovery
{
    // private readonly Device _myDevice;
    // private readonly BonjourDiscovery _innerDiscoveryService;
    private readonly BleDiscovery _bleDiscovery = new();

    public ObservableCollection<Device> DiscoveredDevices { get; }

    public DeviceDiscovery(Device myDevice)
    {
        // _myDevice = myDevice;
        // _innerDiscoveryService = new BonjourDiscovery(myDevice);
        // DiscoveredDevices = _innerDiscoveryService.DiscoveredDevices;
        DiscoveredDevices = _bleDiscovery.DiscoveredDevices;
    }

    public void Browse()
    {
        _bleDiscovery.Browse();
    }
}
