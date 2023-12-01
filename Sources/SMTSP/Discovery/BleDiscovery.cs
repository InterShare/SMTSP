using System.Collections.ObjectModel;
using SMTSP.BluetoothLowEnergy;

namespace SMTSP.Discovery;

public class BleDiscovery
{
    private readonly BleServer _bleServer = new();

    public ObservableCollection<Device> DiscoveredDevices { get; } = [];

    public BleDiscovery()
    {
        _bleServer.PeripheralDataDiscovered += OnPeripheralDataDiscovered;
    }

    public Task RequestAccess()
    {
        return _bleServer.RequestAccess();
    }

    public void Browse()
    {
        _bleServer.StartDiscovering();
    }

    private void OnPeripheralDataDiscovered(object? sender, byte[] rawDeviceData)
    {
        var device = Device.Parser.ParseFrom(rawDeviceData);
        AddOrReplaceDevice(device);
    }

    private void AddOrReplaceDevice(Device device)
    {
        lock (DiscoveredDevices)
        {
            var existingDevice = DiscoveredDevices.FirstOrDefault(element => element.Id == device.Id);

            if (existingDevice == null)
            {
                DiscoveredDevices.Add(device);
            }
            else
            {
                var index = DiscoveredDevices.IndexOf(existingDevice);
                DiscoveredDevices[index] = device;
            }
        }
    }
}
