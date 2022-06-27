using SMTSP.Core;
using SMTSP.Extensions;

namespace SMTSP.Entities;

/// <summary>
/// Details to a SMTSP compatible device.
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// The unique ID of the device
    /// </summary>
    public string DeviceId { get; set; } = null!;

    /// <summary>
    /// The name of the device.
    /// </summary>
    public string DeviceName { get; set; } = null!;

    /// <summary>
    /// Port for the data transfer.
    /// </summary>
    public ushort Port { get; set; }

    /// <summary>
    /// Like computer, phone.
    /// </summary>
    public string DeviceType { get; set; } = null!;

    /// <summary>
    /// Needed for the data transfer.
    /// </summary>
    public string IpAddress { get; set; } = null!;

    /// <summary>
    ///
    /// </summary>
    internal bool ProtocolVersionIncompatible { get; set; } = false;

    /// <summary>
    /// Get Properties from tcp stream.
    /// </summary>
    /// <param name="stream"></param>
    public DeviceInfo(Stream stream)
    {
        FromStream(stream);
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