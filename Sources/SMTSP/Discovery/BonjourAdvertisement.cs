using SMTSP.Bonjour.Providers.Bonjour;
using SMTSP.Core;

namespace SMTSP.Discovery;

public class BonjourAdvertisement
{
    private readonly Device _myDevice;
    private RegisterService? _service;

    public BonjourAdvertisement(Device device)
    {
        _myDevice = device;
    }

    public void Register(ushort port)
    {
        _service = new RegisterService();
        _service.Name = _myDevice.Id;
        _service.RegType = BonjourDiscovery.ServiceName;
        _service.ReplyDomain = "local.";
        _service.UPort = port;

        var txtRecord = new TxtRecord();
        txtRecord.Add(TxtProperties.Name, _myDevice.Name);
        txtRecord.Add(TxtProperties.Type, _myDevice.Type.ToString());
        txtRecord.Add(TxtProperties.Version, Config.ProtocolVersion.ToString());

        _service.TxtRecord = txtRecord;

        _service.Register();
    }

    public void Unregister()
    {
        _service?.Dispose();
    }
}
