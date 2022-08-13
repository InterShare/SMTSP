using System.Collections.ObjectModel;
using SMTSP.Entities;
using DeviceInfo = SMTSP.Entities.DeviceInfo;

namespace SMTSP.Discovery;

internal interface IDiscovery : IDisposable
{
    public ObservableCollection<DeviceInfo> DiscoveredDevices { get; }

    public void SetMyDevice(DeviceInfo myDevice);
    public void StartDiscovering();
    public void StopDiscovering();
    public void Advertise();
    public void StopAdvertising();
}