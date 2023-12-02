using System.Collections.ObjectModel;
using SMTSP.BluetoothLowEnergy;

namespace SMTSP.Discovery;

public class BleDiscovery
{
    private static readonly BleServer BleServer = new();

    public ObservableCollection<Device> DiscoveredDevices { get; } = [];

    public BleDiscovery()
    {
        BleServer.PeripheralDataDiscovered += OnPeripheralDataDiscovered;
    }

    public Task<bool> RequestAccess()
    {
        return BleServer.RequestAccess();
    }

    public static Task<ushort> StartServer()
    {
        return BleServer.StartServer();
    }

    public static void StopServer()
    {
        BleServer.StopServer();
    }

    public static void Browse()
    {
        BleServer.StartDiscovering();
    }

    public static void StopBrowsing()
    {
        BleServer.StopDiscovering();
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
