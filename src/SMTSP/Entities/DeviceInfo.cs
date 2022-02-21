using SMTSP.Core;
using SMTSP.Extensions;

namespace SMTSP.Entities;

/// <summary>
/// Details to a SMTSP compatible device.
/// </summary>
public class DeviceInfo
{

    /// <summary>
    ///
    /// </summary>
    public string DeviceId { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string DeviceName { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int DiscoveryPort { get; set; }

    /// <summary>
    ///
    /// </summary>
    public int TransferPort { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string DeviceType { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string IpAddress { get; set; }

    public DeviceInfo()
    {
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="deviceName"></param>
    /// <param name="discoveryPort"></param>
    /// <param name="transferPort"></param>
    /// <param name="deviceType"></param>
    /// <param name="ipAddress"></param>
    public DeviceInfo(string deviceId, string deviceName, int discoveryPort, int transferPort, string deviceType, string ipAddress)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        DiscoveryPort = discoveryPort;
        TransferPort = transferPort;
        DeviceType = deviceType;
        IpAddress = ipAddress;
    }

    internal byte[] ToBinary()
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

    internal void FromStream(Stream stream)
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