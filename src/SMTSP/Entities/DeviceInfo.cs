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
    public ushort Port { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string DeviceType { get; set; }

    /// <summary>
    ///
    /// </summary>
    public string IpAddress { get; set; }

    /// <summary>
    ///
    /// </summary>
    public bool ProtocolVersionIncompatible { get; set; } = false;

    public DeviceInfo()
    {
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="deviceId"></param>
    /// <param name="deviceName"></param>
    /// <param name="port"></param>
    /// <param name="deviceType"></param>
    /// <param name="ipAddress"></param>
    public DeviceInfo(string deviceId, string deviceName, ushort port, string deviceType, string ipAddress)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        Port = port;
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

        Logger.Info($"TransferPort: {Port}");
        messageInBytes.AddRange(Port.ToString().GetBytes());
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
            Port = ushort.Parse(stream.GetStringTillEndByte(0x00));
            DeviceType = stream.GetStringTillEndByte(0x00);
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
        }
    }
}