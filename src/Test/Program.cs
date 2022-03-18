using SMTSP.Advertisement;
using SMTSP.Core;
using SMTSP.Discovery;
using SMTSP.Discovery.Entities;
using SMTSP.Entities;

var device = new DeviceInfo
{
    DeviceId = Guid.NewGuid().ToString(),
    DeviceName = "SMTS Console Test",
    DeviceType = DeviceTypes.AppleComputer
};

var type = DiscoveryTypes.UdpBroadcasts;

if (args?.Length > 0)
{
    type = args[0] == "mdns" ? DiscoveryTypes.Mdns : DiscoveryTypes.UdpBroadcasts;
}

SmtsConfig.LoggerOutputEnabled = false;

var smtsDiscovery = new Discovery(device, type);
var smtsAdvertisement = new Advertiser(device, type);

smtsDiscovery.DiscoveredDevices.CollectionChanged += delegate
    {
        Console.WriteLine("============");
        foreach (DeviceInfo discoveredDevice in smtsDiscovery.DiscoveredDevices)
        {
            Console.WriteLine($"- {discoveredDevice.DeviceName} ({discoveredDevice.IpAddress})");
        }
        Console.WriteLine("============");
    };

Console.WriteLine("Advertise...");
smtsAdvertisement.Advertise();
smtsDiscovery.SendOutLookupSignal();

Console.ReadKey();
Console.WriteLine("Unadvertise...");
smtsAdvertisement.StopAdvertising();