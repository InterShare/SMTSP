using System.Collections.ObjectModel;
using SMTSP.Bonjour;
using SMTSP.Core;
using SMTSP.Extensions;

namespace SMTSP.Discovery;

internal struct TxtProperties
{
    public const string Name = "Name";
    public const string Type = "Type";
    public const string Version = "Version";
}

/// <summary>
/// This uses the native underlying bonjour libraries to advertise/discover mDNS services.
/// Uses Avahi or Bonjour/mDNSResponder.
/// </summary>
internal class BonjourDiscovery(Device device)
{
    public const string ServiceName = "_smtsp._tcp";
    private readonly Device _myDevice = device;

    public ObservableCollection<Device> DiscoveredDevices { get; } = new();

    public void Browse()
    {
        var browser = new ServiceBrowser();
        browser.ServiceAdded += OnServiceAdded;
        browser.ServiceRemoved += OnServiceRemoved;

        browser.Browse(ServiceName, "local");
    }

    private void OnServiceRemoved(object o, ServiceBrowseEventArgs serviceBrowseEventArgs)
    {
        var device = DiscoveredDevices.FirstOrDefault(device => device.Id == serviceBrowseEventArgs.Service.Name);

        if (device != null)
        {
            DiscoveredDevices.Remove(device);
        }
    }

    private void AddOrReplaceDevice(Device device)
    {
        lock (DiscoveredDevices)
        {
            var existingDevice = DiscoveredDevices.FirstOrDefault(element => element.Id == device.Id);

            if (existingDevice == null)
            {
                DiscoveredDevices.Add(device);
            }
            else
            {
                var index = DiscoveredDevices.IndexOf(existingDevice);
                DiscoveredDevices[index] = device;
            }
        }
    }

    private void OnServiceAdded(object _, ServiceBrowseEventArgs serviceBrowseEventArgs)
    {
        serviceBrowseEventArgs.Service.Resolved += (_, args) => {
            try
            {
                var resolvableService = args.Service;

                var id = resolvableService.Name;

                var protocolVersionRaw = resolvableService.TxtRecord.GetValue(TxtProperties.Version);

                if (string.IsNullOrEmpty(protocolVersionRaw))
                {
                    return;
                }

                var protocolVersion = int.Parse(protocolVersionRaw);
                var name = resolvableService.TxtRecord.GetValue(TxtProperties.Name);
                var type = resolvableService.TxtRecord.GetValue(TxtProperties.Type)?.ToEnum<Device.Types.DeviceType>(Device.Types.DeviceType.Unknown);

                var ipAddress = resolvableService.HostEntry.AddressList.FirstOrDefault();
                var port = (ushort) resolvableService.Port;

                if (string.IsNullOrEmpty(id)
                    || string.IsNullOrEmpty(name)
                    || type == null
                    || ipAddress == null)
                {
                    return;
                }

                AddOrReplaceDevice(new Device
                {
                    Id = id,
                    Name = name,
                    Type = type.Value,
                    ProtocolVersion = protocolVersion,
                    TcpConnectionInfo = new TcpConnectionInfo
                    {
                        Hostname = ipAddress.ToString(),
                        Port = Convert.ToUInt32(port)
                    }
                });
            }
            catch (Exception exception)
            {
                Logger.Exception(exception);
            }
        };

        serviceBrowseEventArgs.Service.Resolve();
    }
}
