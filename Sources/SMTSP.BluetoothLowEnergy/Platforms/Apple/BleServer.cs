using CoreBluetooth;

namespace SMTSP.BluetoothLowEnergy;

public partial class BleServer
{
    private CBPeripheralManager? _manager;
    protected CBPeripheralManager Manager
    {
        get
        {
            // var options = new NSDictionary(
            //     CBPeripheralManager.OptionRestoreIdentifierKey, "com.julian-baumann.smtsp"
            // );

            // _manager ??= new CBPeripheralManager(peripheralDelegate: null, queue: null, options: options);
            _manager ??= new CBPeripheralManager();
            return _manager;
        }
    }

    private ushort _psm;

    public partial Task<bool> RequestAccess()
    {
        Extensions.EnsureAllowed();

        if (Manager.State != CBManagerState.Unknown)
        {
            return Task.FromResult(true);
        }

        var result = new TaskCompletionSource<bool>();

        Manager.StateUpdated += (_, _) =>
        {
            result.SetResult(Manager.State == CBManagerState.PoweredOn);
        };

        _ = Manager.State;

        return result.Task;
    }

    private async Task<ushort> PublishL2Cap(bool secure)
    {
        var taskCompletionSource = new TaskCompletionSource<ushort>();

        var handler = new EventHandler<CBPeripheralManagerL2CapChannelOperationEventArgs>((_, args) =>
        {
            if (args.Error == null)
            {
                taskCompletionSource.TrySetResult(args.Psm);
            }
            else
            {
                taskCompletionSource.TrySetException(new InvalidOperationException(args.Error.Description));
            }
        });

        Manager.DidPublishL2CapChannel += handler;

        try
        {
            Manager.PublishL2CapChannel(secure);
            return await taskCompletionSource.Task;
        }
        finally
        {
            Manager.DidPublishL2CapChannel -= handler;
        }
    }

    private void ManagerOnDidOpenL2CapChannel(object? sender, CBPeripheralManagerOpenL2CapChannelEventArgs args)
    {
        //args.Channel.InputStream.Status == NSStreamStatus.Open
        var channel = args.Channel!;
        channel.InputStream.Open();
        channel.OutputStream.Open();

        ClientConnected.Invoke(this, new L2CapStream(channel.OutputStream, channel.InputStream));
    }

    private void AdvertiseService()
    {
        var service = new CBMutableService(CBUUID.FromString(Core.ServiceUuid), true);

        var characteristic = new CBMutableCharacteristic(
            uuid: CBUUID.FromString(Core.CharacteristicUuid),
            properties: CBCharacteristicProperties.Read | CBCharacteristicProperties.Write,
            value: null,
            permissions: CBAttributePermissions.Readable | CBAttributePermissions.Writeable
        );

        service.Characteristics = [characteristic];

        Manager.WriteRequestsReceived += ManagerOnWriteRequestsReceived;

        Manager.AddService(service);
        Manager.StartAdvertising(new StartAdvertisingOptions
        {
            ServicesUUID = [CBUUID.FromString(Core.ServiceUuid)]
        });
    }

    private void ManagerOnWriteRequestsReceived(object? sender, CBATTRequestsEventArgs args)
    {
        Console.WriteLine("[Server] received write request!");
        Task.Run(() =>
        {
            foreach (var request in args.Requests)
            {
                var value = request.Value;

                if (value == null)
                {
                    return;
                }

                var valueAsByteArray = new byte[value.Length];
                System.Runtime.InteropServices.Marshal.Copy(value.Bytes, valueAsByteArray, 0, Convert.ToInt32(value.Length));

                PeripheralDataDiscovered.Invoke(this, valueAsByteArray);
            }
        });
    }

    public partial async Task<ushort> StartServer()
    {
        Manager.DidOpenL2CapChannel += ManagerOnDidOpenL2CapChannel;
        _psm = await PublishL2Cap(false);

        return _psm;
    }

    public partial void StartDiscovering()
    {
        if (_psm <= 0)
        {
            throw new InvalidOperationException("PSM unknown, did you forget to call StartServer()?");
        }

        AdvertiseService();
    }

    public partial void StopDiscovering()
    {
        Manager.StopAdvertising();
        Manager.RemoveAllServices();
    }

    public partial void StopServer()
    {
        if (_psm <= 0) { return; }

        Manager.UnpublishL2CapChannel(_psm);
        Manager.DidOpenL2CapChannel -= ManagerOnDidOpenL2CapChannel;
    }
}
