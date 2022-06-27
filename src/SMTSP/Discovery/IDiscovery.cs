using System.Collections.ObjectModel;
using SMTSP.Entities;

namespace SMTSP.Discovery;

internal interface IDiscovery : IDisposable
{
    public ObservableCollection<DeviceInfo> DiscoveredDevices { get; }

    public void SetMyDevice(DeviceInfo myDevice);
    public void StartDiscovering();
}