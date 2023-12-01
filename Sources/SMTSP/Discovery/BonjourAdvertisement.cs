using SMTSP.Bonjour.Providers.Bonjour;
using SMTSP.Core;

namespace SMTSP.Discovery;

public class BonjourAdvertisement(Device device)
{
    private RegisterService? _service;

    public void Register(ushort port)
    {
        _service = new RegisterService();
        _service.Name = device.Id;
        _service.RegType = BonjourDiscovery.ServiceName;
        _service.ReplyDomain = "local.";
        _service.UPort = port;

        var txtRecord = new TxtRecord();
        txtRecord.Add(TxtProperties.Name, device.Name);
        txtRecord.Add(TxtProperties.Type, device.Type.ToString());
        txtRecord.Add(TxtProperties.Version, Config.ProtocolVersion.ToString());

        _service.TxtRecord = txtRecord;

        _service.Register();
    }

    public void Unregister()
    {
        _service?.Dispose();
    }
}
