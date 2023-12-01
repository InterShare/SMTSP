namespace SMTSP.BluetoothLowEnergy;

public partial class BleClient(byte[] deviceData)
{
    public bool IsResponding { get; private set; }

    public partial Task<bool> RequestAccess();
    public partial void StartRespondingToDiscoveryBroadcasts();
    public partial void StopRespondingToDiscoveryBroadcasts();
}
