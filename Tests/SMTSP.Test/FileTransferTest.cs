using System.Net;
using Google.Protobuf;
using NUnit.Framework;
using SMTSP.Communication;
using SMTSP.Discovery;

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
            IpAddress = IPAddress.Loopback.ToString()
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

    [Test]
    public void TestTest()
    {
        var array = new byte[]
        {
            0, 1, 2, 3
        };
        var encryptionRequest = new EncryptionRequest
        {
            PublicKey = ByteString.CopyFrom(array)
        }.ToByteArray();

        var stream = new MemoryStream(encryptionRequest);

        var test = EncryptionRequest.Parser.ParseFrom(stream);
    }
}
