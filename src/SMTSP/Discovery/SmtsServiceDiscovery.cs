using Makaretu.Dns;
using MDNS;

namespace SMTSP.Discovery;

public class SmtsServiceDiscovery
{
    private const string ServiceName = "_smtsp._tcp";

    private readonly ServiceDiscovery _serviceDiscovery;
    private readonly string _deviceName;

    private ServiceProfile? _serviceProfile;

    public SmtsServiceDiscovery(string deviceName)
    {
        _deviceName = deviceName;
        _serviceDiscovery = new ServiceDiscovery();
    }

    public void AdvertiseService()
    {
        _serviceProfile = new ServiceProfile(_deviceName, ServiceName, 5010);
        _serviceDiscovery.Advertise(_serviceProfile);
        _serviceDiscovery.Announce(_serviceProfile);
        Console.WriteLine("Advertising and announcing");
    }

    public void RemoveService()
    {
        if (_serviceProfile != null)
        {
            _serviceDiscovery.Unadvertise(_serviceProfile);
        }
    }

    public void ListenForServices()
    {
        _serviceDiscovery.ServiceInstanceDiscovered += (s, serviceName) =>
        {
            if (serviceName.ServiceInstanceName.ToString().Contains(ServiceName))
            {
                Console.WriteLine($"Discovered {serviceName.ServiceInstanceName.Labels.FirstOrDefault()}");
            }
        };

        _serviceDiscovery.ServiceInstanceShutdown += (s, serviceName) =>
        {
            if (serviceName.ServiceInstanceName.ToString().Contains(ServiceName))
            {
                Console.WriteLine($"{serviceName.ServiceInstanceName.Labels.FirstOrDefault()} is now offline");
            }
        };

        // _mdns.NetworkInterfaceDiscovered += (s, e) => _mdns.SendQuery("_foobar._udp.local");
        // _mdns.AnswerReceived += (s, e) =>
        // {
        //     Console.WriteLine($"Answer received");
        // };

        _serviceDiscovery.QueryServiceInstances(new DomainName(ServiceName));
        Console.WriteLine("Listening for services...");
    }
}