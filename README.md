# SMTSP - Send Me That Shit Protocol

> Very work in progress. Shouldn't be used in production just jet.

[![Build](https://github.com/InterShare/SMTSP/actions/workflows/build.yml/badge.svg)](https://github.com/InterShare/SMTSP/actions/workflows/build.yml)

Available on [NuGet](https://www.nuget.org/packages/SMTSP/).


The SMTS-Protocol is what powers InterShare. It allows us to send data between devices in a very high level way. So we don't have to care how data is transferred. 
This protocol includes a discovery-service, which is used to discover nearby devices, and the actual transfer-service to send the data to the desired peripheral.

## Examples

Discovering devices:
```csharp
var myDevice = new DeviceInfo(
  deviceId: "EE27A6ED-6F30-4299-A35F-AC3B7139F733",
  deviceName: "My Device",
  port: 42013,
  deviceType: DeviceTypes.Phone,
  ipAddress: "192.168.1.42",
  capabilities: new[] { "InterShare" }
);

var discovery = new DeviceDiscovery(myDevice);
disocvery.StartDiscovering();
discovery.DiscoveredDevices.CollectionChanged += (sender, args) =>
{
    // Some device was discovered.
};
```

Advertise a device:
```csharp
var discovery = new DeviceDiscovery(myDevice);
discovery.Advertise();
```

Sending a file:
```csharp
FileStream file = File.OpenRead("file.txt");

var content = new SmtspFileContent
{
    FileName = "SomeFile.txt",
    DataStream = file
};

await SmtspSender.Send(_receiver, content, _sender);
```

Receiving a file:
```csharp
var receiver = new SmtspReceiver();
receiver.StartReceiving();
receiver.RegisterTransferRequestCallback((TransferRequest request) => Task.FromResult(true));
receiver.OnContentReceive += (sender, content) =>
{
    FileStream stream = File.OpenWrite("File.txt");
    content.DataStream?.CopyTo(stream);
};
```

## Discovery

To increase the chance of discovering devices, SMTSP uses multiple technologies to advertise and discover peripherals. <br />
The first method uses `UDP Broadcasts` to send out lookup signals and listen for responses. The second method uses `MDNS-Service Discovery`.

## Transfer

To transfer the data, the current implementation only uses a `TCP Socket`. Future plans also include a Bluetooth discovery and transfer.
