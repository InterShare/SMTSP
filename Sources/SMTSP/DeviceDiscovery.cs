using System.Collections.ObjectModel;
using SMTSP.Discovery;

namespace SMTSP;

public class DeviceDiscovery
{
    private readonly Device _myDevice;
    private readonly BonjourDiscovery _innerDiscoveryService;

    public ObservableCollection<Device> DiscoveredDevices { get; }

    public DeviceDiscovery(Device myDevice)
    {
        _myDevice = myDevice;
        _innerDiscoveryService = new BonjourDiscovery(myDevice);
        DiscoveredDevices = _innerDiscoveryService.DiscoveredDevices;
    }

    public void Browse()
    {
        _innerDiscoveryService.Browse();
    }
}
