using SMTSP.Discovery;
using SMTSP.Discovery.Entities;
using SMTSP.Entities;

var device = new DeviceInfo
{
    DeviceId = "c717495a-f79c-4794-ac7e-f390beae014c",
    DeviceName = "SMTS Console Test",
    DeviceType = DeviceTypes.AppleComputer
};

var smtsDiscovery = new Discovery(device);
var smtsAdvertisement = new Advertiser(device);

smtsDiscovery.DiscoveredDevices.CollectionChanged += delegate
    {
        Console.WriteLine("============");
        foreach (DeviceInfo device in smtsDiscovery.DiscoveredDevices)
        {
            Console.WriteLine($"- {device.DeviceName}");
        }
        Console.WriteLine("============");
    };

Console.WriteLine("Advertise...");
smtsAdvertisement.Advertise();
smtsDiscovery.SendOutLookupSignal();

Console.ReadKey();
Console.WriteLine("Unadvertise...");
smtsAdvertisement.StopAdvertising();