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

    public Task<bool> RequestAccess()
    {
        return _bleDiscovery.RequestAccess();
    }

    public void Browse()
    {
        BleDiscovery.Browse();
    }

    public void StartServer()
    {
        BleDiscovery.StartServer();
    }

    public void StopServer()
    {
        BleDiscovery.StopServer();
    }

    public void StopBrowsing()
    {
        BleDiscovery.StopBrowsing();
    }
}
