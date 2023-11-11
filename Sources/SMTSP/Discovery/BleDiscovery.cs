using Plugin.BLE;

namespace SMTSP.Discovery;

public class BleDiscovery
{
    public BleDiscovery()
    {
        var ble = CrossBluetoothLE.Current;
        var adapter = CrossBluetoothLE.Current.Adapter;
    }
}
