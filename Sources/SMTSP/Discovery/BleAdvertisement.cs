using Google.Protobuf;
using SMTSP.BluetoothLowEnergy;

namespace SMTSP.Discovery;

public class BleAdvertisement(Device myDevice)
{
    private readonly BleClient _bleClient = new(myDevice.ToByteArray());

    public Task<bool> RequestAccess()
    {
        return _bleClient.RequestAccess();
    }

    public void StartAdvertising()
    {
        _bleClient.StartRespondingToDiscoveryBroadcasts();
    }

    public void StopAdvertising()
    {
        _bleClient.StopRespondingToDiscoveryBroadcasts();
    }
}
