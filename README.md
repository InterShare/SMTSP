# SMTSP - Send Me That Shit Protocol

> Very work in progress. Shouldn't be used in production just jet.

[![Build](https://github.com/InterShare/SMTSP/actions/workflows/build.yml/badge.svg)](https://github.com/InterShare/SMTSP/actions/workflows/build.yml)

Available on [NuGet](https://www.nuget.org/packages/SMTSP/).


The SMTS-Protocol is what powers InterShare. It allows us to send data between devices in a very high level way. So we don't have to care how data is transferred.

Devices are identified through the Bonjour protocol. For the actual data transmission, SMTSP initiates an SSL connection with the designated peripheral over TCP.

## Examples

Discover nearby devices:
```csharp
var myDevice = new Device
{
    Name = "My Device",
    Id = "EE27A6ED-6F30-4299-A35F-AC3B7139F733",
    Type = Device.Types.DeviceType.Mobile,
    TcpConnectionInfo = new TcpConnectionInfo
    {
        Hostname = Dns.GetHostName()
    }
};

var disocvery = new DeviceDiscovery(myDevice);
discovery.DiscoveredDevices.CollectionChanged += (sender, args) =>
{
    // Some device was discovered.
};

disocvery.Browse();
```

Start a nearby server and advertise the device on the network:
```csharp
var certificate = EncryptionHelper.GenerateSelfSignedCertificate();
var myDevice = new Device
{
    Name = "My Device",
    Id = "EE27A6ED-6F30-4299-A35F-AC3B7139F733",
    Type = Device.Types.DeviceType.Mobile,
    TcpConnectionInfo = new TcpConnectionInfo
    {
        Hostname = Dns.GetHostName()
    }
};

var discovery = new NearbyCommunication(myDevice, certificate);
discovery.AdvertiseDevice();
```

Send a file:
```csharp
var fileStream = File.OpenRead("./TestFile.txt");
var fileInfo = new SharedFileInfo
{
    FileName = "TestFile.txt",
    FileSize = fileStream.Length
};

await nearby.SendFile(ServerDevice, fileInfo, fileStream);
```

Receive file:
```csharp
nearbyServer.OnConnectionRequest += (_, transferRequest) =>
    {
        transferRequest.Accept();

        if (transferRequest is FileTransfer fileTransfer)
        {
            var fileStream = fileTransfer.GetFile();

            using var newFile = File.OpenWrite(ReceivedFilePath);
            fileStream.CopyTo(newFile);
            newFile.Close();
        }
    };


await nearbyServer.StartReceiving();
```