using NUnit.Framework;
using SMTSP.Protocol.Communication;
using SMTSP.Protocol.Discovery;

namespace SMTSP.Test;

public class FileTransferTest
{
    private static readonly Device ServerDevice = new Device
    {
        Name = "Server [TEST]",
        Id = "1",
        Type = Device.Types.DeviceType.Mobile,
        TcpConnectionInfo = new TcpConnectionInfo
        {
            IpAddress = "127.0.0.1"
        }
    };

    [SetUp]
    public async Task SetupServer()
    {
        var nearby = new NearbyCommunication(ServerDevice);
        nearby.OnConnectionRequest += (_, transferRequest) =>
        {
            transferRequest.Accept();
        };

        await nearby.StartReceiving();
    }

    [Test]
    public async Task TestFileTransfer()
    {
        var device = new Device
        {
            Name = "Sender [TEST]",
            Id = "2",
            Type = Device.Types.DeviceType.Mobile
        };

        var nearby = new NearbyCommunication(device);

        var fileStream = File.OpenRead("./TestFile.txt");
        var fileInfo = new SharedFileInfo
        {
            FileName = "TestFile.txt",
            FileSize = fileStream.Length
        };

        await nearby.SendFile(ServerDevice, fileInfo, fileStream);
    }
}
