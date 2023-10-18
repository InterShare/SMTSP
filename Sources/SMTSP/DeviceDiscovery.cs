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

    public void Register()
    {
        if (_myDevice.TcpConnectionInfo == null || _myDevice.TcpConnectionInfo.Port == 0)
        {
            throw new NullReferenceException("TCP Port is unknown. Did you forget to start the NearbyCommunication server?");
        }

        _innerDiscoveryService.Register((ushort) _myDevice.TcpConnectionInfo.Port);
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
