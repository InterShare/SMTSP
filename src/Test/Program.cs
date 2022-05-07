using System.Text;
using SMTSP;
using SMTSP.Advertisement;
using SMTSP.Core;
using SMTSP.Discovery;
using SMTSP.Discovery.Entities;
using SMTSP.Entities;
using SMTSP.Entities.Content;

//
// var device = new DeviceInfo
// {
//     DeviceId = Guid.NewGuid().ToString(),
//     DeviceName = "SMTS Console Test",
//     DeviceType = DeviceTypes.AppleComputer
// };
//
// var type = DiscoveryTypes.UdpBroadcasts;
//
// if (args?.Length > 0)
// {
//     type = args[0] == "mdns" ? DiscoveryTypes.Mdns : DiscoveryTypes.UdpBroadcasts;
// }
//
// SmtsConfig.LoggerOutputEnabled = false;
//
// var smtsDiscovery = new Discovery(device, type);
// var smtsAdvertisement = new Advertiser(device, type);
//
// smtsDiscovery.DiscoveredDevices.CollectionChanged += delegate
//     {
//         Console.WriteLine("============");
//         foreach (DeviceInfo discoveredDevice in smtsDiscovery.DiscoveredDevices)
//         {
//             Console.WriteLine($"- {discoveredDevice.DeviceName} ({discoveredDevice.IpAddress})");
//         }
//         Console.WriteLine("============");
//     };
//
// Console.WriteLine("Advertise...");
// smtsAdvertisement.Advertise();
// smtsDiscovery.SendOutLookupSignal();
//
// Console.ReadKey();
// Console.WriteLine("Unadvertise...");
// smtsAdvertisement.StopAdvertising();

var test = new Test()
{
    Name = "Hello World",
    Size = 23,
    TypeEnum = TestEnum.TwoValue,
    DataStream = new MemoryStream(Encoding.UTF8.GetBytes("hello universe!!!"))
};

SmtspReceiver receiver = new SmtspReceiver();
receiver.RegisterTransferRequestCallback(request =>
{
    return Task.FromResult(true);
});

receiver.OnFileReceive += (sender, content) =>
{
    Console.WriteLine(((Test) content).Name);
    Console.WriteLine(((Test) content).Size);
    Console.WriteLine(((Test) content).TypeEnum);

    // var stream = new MemoryStream();
    //
    // ?.CopyTo(stream);
    StreamReader reader = new StreamReader(content.DataStream);

    string text = reader.ReadToEnd();
    Console.WriteLine(text);
};
receiver.StartReceiving();

DeviceInfo device = new DeviceInfo
{
    DeviceId = Guid.NewGuid().ToString(),
    DeviceName = "SMTS Console Test",
    DeviceType = DeviceTypes.AppleComputer,
    Port = receiver.Port,
    IpAddress = "192.168.42.46"
};

await SmtspSender.SendFile(device, test, device);


Console.ReadLine();


// var binary = test.ToBinary();
//
// var testTwo = new Test();
// testTwo.FromStream(new MemoryStream(binary.ToArray()));
// Console.WriteLine(testTwo.Name);
// Console.WriteLine(testTwo.Size);
// Console.WriteLine(testTwo.TypeEnum);

enum TestEnum
{
    Unknown,
    OneValue,
    TwoValue
}

[SmtsContent(nameof(Test))]
class Test : SmtspContent
{
    [IncludeInBody]
    public string Name { get; set; }

    [IncludeInBody]
    public long Size { get; set; }

    [IncludeInBody]
    public TestEnum TypeEnum { get; set; }

    public string HelloTestProperty { get; set; }
}