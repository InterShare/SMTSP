using Google.Protobuf;
using SMTSP.BluetoothLowEnergy;

namespace SMTSP.Discovery;

public class BleAdvertisement
{
    private readonly BleClient _bleClient;

    public BleAdvertisement(Device myDevice)
    {
        using var output = new MemoryStream();
        myDevice.WriteDelimitedTo(output);
        _bleClient = new BleClient(output.ToArray());
    }

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
