// See https://aka.ms/new-console-template for more information

using SMTSP.Discovery;

var smtsDiscovery = new SmtsServiceDiscovery("SMTSP .NET Test");
Console.WriteLine("Advertise...");
smtsDiscovery.AdvertiseService();
smtsDiscovery.ListenForServices();

Console.ReadKey();
Console.WriteLine("Unadvertise...");
smtsDiscovery.RemoveService();