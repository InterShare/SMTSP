using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using Makaretu.Dns;
using MDNS;
using SMTSP.Core;
using SMTSP.Entities;

namespace SMTSP.Discovery;

internal class MdnsDiscovery : IDiscovery
{
    private DeviceInfo _myDevice = null!;
    private ServiceDiscovery _serviceDiscovery = null!;

    public ObservableCollection<DeviceInfo> DiscoveredDevices { get; } = new ObservableCollection<DeviceInfo>();

    public void SetMyDevice(DeviceInfo myDevice)
    {
        _myDevice = myDevice;
        _serviceDiscovery = new ServiceDiscovery();

        _serviceDiscovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown += OnServiceInstanceShutdown;
        _serviceDiscovery.Mdns.AnswerReceived += OnAnswerReceived;

        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
    }

    private void OnServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs eventArgs)
    {
        try
        {
            if (eventArgs.ServiceInstanceName.ToString().Contains(SmtsConfiguration.ServiceName)
                && !eventArgs.ServiceInstanceName.ToString().StartsWith(_myDevice.DeviceId))
            {
                TXTRecord? txtRecord = eventArgs.Message?.Answers.OfType<TXTRecord>().FirstOrDefault();

                if (txtRecord == null)
                {
                    string? service = eventArgs.ServiceInstanceName.ToString();
                    var query = new Message();
                    query.Questions.Add(new Question { Name = service, Type = DnsType.TXT });
                    _serviceDiscovery.Mdns.SendQuery(query);

                    return;
                }

                GetDeviceFromRecords(txtRecord, eventArgs.RemoteEndPoint!);
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private void OnServiceInstanceShutdown(object? sender, ServiceInstanceShutdownEventArgs eventArgs)
    {
        if (eventArgs.ServiceInstanceName.ToString().Contains(SmtsConfiguration.ServiceName))
        {
            DeviceInfo? existingDevice = DiscoveredDevices.FirstOrDefault(element => element.DeviceId == eventArgs.ServiceInstanceName.Labels[0]);

            if (existingDevice != null)
            {
                lock (DiscoveredDevices)
                {
                    DiscoveredDevices.Remove(existingDevice);
                }
            }
        }
    }

    private void OnAnswerReceived(object? sender, MessageEventArgs args)
    {
        TXTRecord? txtRecord = args.Message.Answers.OfType<TXTRecord>().FirstOrDefault();

        if (txtRecord != null && txtRecord.Name.ToString().Contains(SmtsConfiguration.ServiceName) && !txtRecord.Name.ToString().StartsWith(_myDevice.DeviceId))
        {
            GetDeviceFromRecords(txtRecord, args.RemoteEndPoint);
        }
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        lock (DiscoveredDevices)
        {
            DiscoveredDevices.Clear();
        }

        StartDiscovering();
    }

    private static string? GetPropertyValueFromTxtRecords(List<string> records, string propertyName)
    {
        if (!records.Any())
        {
            return null;
        }

        string? property = records.Find(element => element.StartsWith($"{propertyName}="));

        return property?.Remove(0, propertyName.Length + 1);
    }

    private void GetDeviceFromRecords(TXTRecord record, IPEndPoint ipEndPoint)
    {
        try
        {
            string? deviceId = GetPropertyValueFromTxtRecords(record.Strings, "deviceId");

            if (deviceId == _myDevice.DeviceId)
            {
                return;
            }

            string? deviceName = GetPropertyValueFromTxtRecords(record.Strings, "deviceName");
            string? deviceType = GetPropertyValueFromTxtRecords(record.Strings, "type");
            string? protocolVersionString = GetPropertyValueFromTxtRecords(record.Strings, "smtspVersion");
            string? portString = GetPropertyValueFromTxtRecords(record.Strings, "port");
            string? capabilities = GetPropertyValueFromTxtRecords(record.Strings, "capabilities");

            if (string.IsNullOrEmpty(deviceId) ||
                string.IsNullOrEmpty(deviceName) ||
                string.IsNullOrEmpty(deviceType) ||
                string.IsNullOrEmpty(protocolVersionString) ||
                string.IsNullOrEmpty(portString) ||
                string.IsNullOrEmpty(capabilities))
            {
                return;
            }

            ushort protocolVersion = ushort.Parse(protocolVersionString);

            lock (DiscoveredDevices)
            {
                DeviceInfo? existingDevice = DiscoveredDevices.FirstOrDefault(element => element.DeviceId == deviceId);

                if (existingDevice == null)
                {
                    DiscoveredDevices.Add(new DeviceInfo(
                        deviceId,
                        deviceName,
                        ushort.Parse(portString),
                        deviceType,
                        ipEndPoint.Address.ToString(),
                        capabilities.Split(", "))
                    {
                        ProtocolVersionIncompatible = protocolVersion != SmtsConfiguration.ProtocolVersion
                    });
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    public void StartDiscovering()
    {
        const string service = "_smtsp._tcp.local";
        var query = new Message();
        query.Questions.Add(new Question { Name = service, Type = DnsType.PTR });
        query.Questions.Add(new Question { Name = service, Type = DnsType.TXT });

        _serviceDiscovery.Mdns.SendQuery(query);
    }

    public void Dispose()
    {
        _serviceDiscovery.ServiceInstanceDiscovered -= OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown -= OnServiceInstanceShutdown;
        _serviceDiscovery.Mdns.AnswerReceived -= OnAnswerReceived;
        NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
        _serviceDiscovery.Dispose();
    }
}