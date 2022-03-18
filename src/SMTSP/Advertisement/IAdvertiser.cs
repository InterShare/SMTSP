using SMTSP.Entities;

namespace SMTSP.Advertisement;

internal interface IAdvertiser : IDisposable
{
    public void SetMyDevice(DeviceInfo myDevice);
    public void Advertise();
    public void StopAdvertising();
}