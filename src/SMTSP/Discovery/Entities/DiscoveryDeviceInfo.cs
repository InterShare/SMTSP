using SMTSP.Core;
using SMTSP.Entities;
using SMTSP.Extensions;

namespace SMTSP.Discovery.Entities;

public class DiscoveryDeviceInfo
{
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public int DiscoveryPort { get; set; }
    public int TransferPort { get; set; }
    public string DeviceType { get; set; }

    public string IpAddress { get; set; }

    public byte[] ToBinary()
    {
        var messageInBytes = new List<byte>();

        messageInBytes.AddSmtsHeader(MessageTypes.DeviceInfo);

        Logger.Info($"DeviceId: {DeviceId}");
        messageInBytes.AddRange(DeviceId.GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"DeviceName: {DeviceName}");
        messageInBytes.AddRange(DeviceName.GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"DiscoveryPort: {DiscoveryPort}");
        messageInBytes.AddRange(DiscoveryPort.ToString().GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"TransferPort: {TransferPort}");
        messageInBytes.AddRange(TransferPort.ToString().GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"DeviceType: {DeviceType}");
        messageInBytes.AddRange(DeviceType.GetBytes());
        messageInBytes.Add(0x00);

        return messageInBytes.ToArray();
    }

    public void FromStream(Stream stream)
    {
        try
        {
            DeviceId = stream.GetStringTillEndByte(0x00);
            DeviceName = stream.GetStringTillEndByte(0x00);
            DiscoveryPort = int.Parse(stream.GetStringTillEndByte(0x00));
            TransferPort = int.Parse(stream.GetStringTillEndByte(0x00));
            DeviceType = stream.GetStringTillEndByte(0x00);
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
        }
    }
}