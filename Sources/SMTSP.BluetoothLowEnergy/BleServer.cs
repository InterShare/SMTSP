namespace SMTSP.BluetoothLowEnergy;

public partial class BleServer
{
    public event EventHandler<byte[]> PeripheralDataDiscovered = delegate { };
    public event EventHandler<Stream> ClientConnected = delegate { };

    public partial Task<bool> RequestAccess();

    public partial Task<ushort> StartServer();
    public partial void StopServer();

    public partial void StartDiscovering();
    public partial void StopDiscovering();
}
