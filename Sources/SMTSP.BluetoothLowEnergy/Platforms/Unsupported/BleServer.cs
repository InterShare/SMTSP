namespace SMTSP.BluetoothLowEnergy;

public partial class BleServer
{
    public partial Task<bool> RequestAccess() => throw new PlatformNotSupportedException();

    public partial Task<ushort> StartServer() => throw new PlatformNotSupportedException();
    public partial void StopServer() => throw new PlatformNotSupportedException();

    public partial void StartDiscovering() => throw new PlatformNotSupportedException();
    public partial void StopDiscovering() => throw new PlatformNotSupportedException();
}
