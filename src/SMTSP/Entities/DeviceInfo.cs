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
    /// Which services does this device host.
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();

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
    /// <param name="capabilities"></param>
    public DeviceInfo(string deviceId, string deviceName, ushort port, string deviceType, string ipAddress, string[] capabilities)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        Port = port;
        DeviceType = deviceType;
        IpAddress = ipAddress;
        Capabilities = capabilities;
    }

    internal byte[] ToBinary()
    {
        var messageInBytes = new List<byte>();

        messageInBytes.AddSmtsHeader(MessageTypes.DeviceInfo);

        messageInBytes.AddRange(DeviceId.GetBytes());
        messageInBytes.Add(0x00);

        messageInBytes.AddRange(DeviceName.GetBytes());
        messageInBytes.Add(0x00);

        messageInBytes.AddRange(Port.ToString().GetBytes());
        messageInBytes.Add(0x00);

        messageInBytes.AddRange(DeviceType.GetBytes());
        messageInBytes.Add(0x00);

        messageInBytes.AddRange(string.Join(", ", Capabilities).GetBytes());
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

            string capabilities = stream.GetStringTillEndByte(0x00);
            Capabilities = capabilities.Split(", ");
        }
        catch (Exception exception)
        {
            Logger.Exception(exception);
        }
    }
}