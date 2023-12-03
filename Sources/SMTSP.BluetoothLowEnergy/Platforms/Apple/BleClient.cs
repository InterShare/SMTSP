using CoreBluetooth;

namespace SMTSP.BluetoothLowEnergy;

public partial class BleClient : CBCentralManagerDelegate
{
    private CBCentralManager? _manager;
    private readonly TaskCompletionSource<bool> _stateTaskCompletionSource = new();
    private readonly NSData _deviceData = NSData.FromArray(deviceData);
    private readonly CBUUID _nativeServiceUuid = CBUUID.FromString(Core.ServiceUuid);
    private readonly CBUUID _nativeCharacteristicUuid = CBUUID.FromString(Core.CharacteristicUuid);

    public CBCentralManager Manager
    {
        get
        {
            if (_manager == null)
            {
                _manager = new CBCentralManager(this, null);
                _manager.Delegate = this;
            }

            return _manager;
        }
    }

    public partial Task<bool> RequestAccess()
    {
        Extensions.EnsureAllowed();

        if (Manager.State != CBManagerState.Unknown)
        {
            return Task.FromResult(true);
        }

        _ = Manager.State;

        return _stateTaskCompletionSource.Task;
    }

    public partial void StartRespondingToDiscoveryBroadcasts()
    {
        if (Manager.IsScanning)
        {
            return;
        }

        Manager.ScanForPeripherals([_nativeServiceUuid], new PeripheralScanningOptions
        {
            AllowDuplicatesKey = true
        });

        IsResponding = true;
    }

    public partial void StopRespondingToDiscoveryBroadcasts()
    {
        IsResponding = false;
        Manager.StopScan();
    }

    public override void UpdatedState(CBCentralManager central)
    {
        _stateTaskCompletionSource.TrySetResult(Manager.State == CBManagerState.PoweredOn);
    }

    public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
    {
        Task.Run(() =>
        {
            if (!IsResponding)
            {
                return;
            }

            Manager.ConnectPeripheral(peripheral);
        });
    }

    private void PeripheralOnDiscoveredService(object? sender, NSErrorEventArgs e)
    {
        Console.WriteLine("Discovered service!");
        Task.Run(() =>
        {
            if (sender is not CBPeripheral cbPeripheral)
            {
                return;
            }

            var service = cbPeripheral.Services?.FirstOrDefault(
                element => element.UUID == _nativeServiceUuid
            );

            if (service == null)
            {
                return;
            }

            cbPeripheral.DiscoverCharacteristics([_nativeCharacteristicUuid], service);
        });
    }

    private void PeripheralOnDiscoveredCharacteristics(object? sender, CBServiceEventArgs args)
    {
        Console.WriteLine("Discovered characteristics!");

        Task.Run(() =>
        {
            if (IsResponding)
            {
                var characteristic = args.Service.Characteristics?.FirstOrDefault(element =>
                    element.UUID == _nativeCharacteristicUuid);

                if (characteristic == null)
                {
                    return;
                }

                args.Service.Peripheral?.WriteValue(_deviceData, characteristic, CBCharacteristicWriteType.WithoutResponse);
            }
        });
    }

    public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral nativePeripheral)
    {
        Console.WriteLine("Connected to peripheral");
        nativePeripheral.DiscoveredService += PeripheralOnDiscoveredService;
        nativePeripheral.DiscoveredCharacteristics += PeripheralOnDiscoveredCharacteristics;

        nativePeripheral.DiscoverServices([_nativeServiceUuid]);
    }
}
