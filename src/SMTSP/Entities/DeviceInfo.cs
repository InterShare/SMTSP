using SMTSP.Extensions;

namespace SMTSP.Entities;

/// <summary>
/// Details to a SMTSP compatible device.
/// </summary>
public class DeviceInfo
{
    /// <summary>
    /// The unique ID of the device.
    /// </summary>
    public string DeviceId { get; set; } = null!;

    /// <summary>
    /// The name of the device.
    /// </summary>
    public string DeviceName { get; set; } = null!;


    /// <summary>
    /// Like computer, phone.
    /// </summary>
    public string DeviceType { get; set; } = null!;

    /// <summary>
    /// Needed for the data transfer.
    /// </summary>
    public string IpAddress { get; set; } = null!;

    /// <summary>
    /// Port for the data transfer.
    /// </summary>
    public ushort TcpPort { get; set; }

    /// <summary>
    /// The services this host provides. Could be the name of your app,
    /// if you only want to communicate with other devices running your app.
    /// </summary>
    public string[] Capabilities { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Assign properties from a stream.
    /// </summary>
    /// <param name="stream"></param>
    public DeviceInfo(Stream stream)
    {
        FromStream(stream);
    }

    /// <param name="deviceId"></param>
    /// <param name="deviceName"></param>
    /// <param name="tcpPort"></param>
    /// <param name="deviceType"></param>
    /// <param name="ipAddress"></param>
    /// <param name="capabilities"></param>
    public DeviceInfo(string deviceId, string deviceName, ushort tcpPort, string deviceType, string ipAddress, string[] capabilities)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        TcpPort = tcpPort;
        DeviceType = deviceType;
        IpAddress = ipAddress;
        Capabilities = capabilities;
    }

    /// <param name="deviceId"></param>
    /// <param name="deviceName"></param>
    /// <param name="deviceType"></param>
    /// <param name="capabilities"></param>
    public DeviceInfo(string deviceId, string deviceName, string deviceType, string[] capabilities)
    {
        DeviceId = deviceId;
        DeviceName = deviceName;
        DeviceType = deviceType;
        Capabilities = capabilities;
    }

    internal byte[] ToBinary()
    {
        var messageInBytes = new List<byte>();

        messageInBytes.AddSmtsHeader(MessageTypes.DeviceInfo);
        
        messageInBytes.AddProperty(nameof(DeviceId), DeviceId);
        messageInBytes.AddProperty(nameof(DeviceName), DeviceName);
        messageInBytes.AddProperty(nameof(TcpPort), TcpPort.ToString());
        messageInBytes.AddProperty(nameof(DeviceType), DeviceType);
        messageInBytes.AddProperty(nameof(Capabilities), string.Join(", ", Capabilities));

        return messageInBytes.ToArray();
    }

    internal void FromStream(Stream stream)
    {
        var properties = stream.GetProperties();

        string? deviceId = properties.GetValueOrDefault("DeviceId");
        string? deviceName = properties.GetValueOrDefault("DeviceName");
        string? tcpPort = properties.GetValueOrDefault("TcpPort");
        string? deviceType = properties.GetValueOrDefault("DeviceType");
        string? capabilities = properties.GetValueOrDefault("Capabilities");

        if (!string.IsNullOrEmpty(deviceId)
            && !string.IsNullOrEmpty(deviceName)
            && !string.IsNullOrEmpty(tcpPort)
            && !string.IsNullOrEmpty(deviceType)
            && !string.IsNullOrEmpty(capabilities))
        {
            DeviceId = deviceId;
            DeviceName = deviceName;
            TcpPort = ushort.Parse(tcpPort);
            DeviceType = deviceType;
            Capabilities = capabilities.Split(", ");
        }
    }
}