using System.Collections.ObjectModel;
using ArkaneSystems.Arkane.Zeroconf;
using SMTSP.Extensions;
using SMTSP.Protocol.Discovery;

namespace SMTSP.Discovery;

/// <summary>
/// This uses the native underlying bonjour libraries to advertise/discover mDNS services.
/// Uses Avahi or Bonjour/mDNSResponder.
/// </summary>
internal class BonjourDiscovery
{
    private const string ServiceName = "_smtsp._tcp";
    private readonly Device _myDevice;

    private RegisterService? _service;

    public ObservableCollection<Device> DiscoveredDevices { get; } = new();

    public BonjourDiscovery(Device device)
    {
        _myDevice = device;
    }

    public void Register(ushort port)
    {
        _service = new RegisterService();
        _service.Name = _myDevice.Id;
        _service.RegType = ServiceName;
        _service.ReplyDomain = "local.";
        _service.UPort = port;

        var txtRecord = new TxtRecord();
        txtRecord.Add("Name", _myDevice.Name);
        txtRecord.Add("Type", _myDevice.Type.ToString());

        _service.TxtRecord = txtRecord;

        _service.Register();
    }

    public void Unregister()
    {
        _service?.Dispose();
    }

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

    private static string? GetPropertyValueFromTxtRecords(ITxtRecord records, string propertyName)
    {
        if (records.Count <= 0)
        {
            return null;
        }

        foreach (TxtRecordItem txtRecord in records)
        {
            if (txtRecord.Key == propertyName)
            {
                return txtRecord.ValueString;
            }
        }

        return null;
    }

    private void OnServiceAdded(object _, ServiceBrowseEventArgs serviceBrowseEventArgs)
    {
        serviceBrowseEventArgs.Service.Resolved += (_, args) => {
            var resolvableService = args.Service;

            var id = resolvableService.Name;
            var name = GetPropertyValueFromTxtRecords(resolvableService.TxtRecord, "Name");
            var type = GetPropertyValueFromTxtRecords(resolvableService.TxtRecord, "Type")?.ToEnum<Device.Types.DeviceType>(Device.Types.DeviceType.Unknown);
            var ipAddress = resolvableService.HostEntry.AddressList.FirstOrDefault();
            var port = resolvableService.Port;

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
                TcpConnectionInfo = new TcpConnectionInfo
                {
                    IpAddress = ipAddress.ToString(),
                    Port = Convert.ToUInt32(port)
                }
            });
        };

        serviceBrowseEventArgs.Service.Resolve();
    }
}
