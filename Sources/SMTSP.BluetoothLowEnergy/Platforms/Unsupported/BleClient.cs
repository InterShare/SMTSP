namespace SMTSP.BluetoothLowEnergy;

public partial class BleClient
{
    public partial Task<bool> RequestAccess() => throw new PlatformNotSupportedException();
    public partial void StartRespondingToDiscoveryBroadcasts() => throw new PlatformNotSupportedException();
    public partial void StopRespondingToDiscoveryBroadcasts() => throw new PlatformNotSupportedException();
}
