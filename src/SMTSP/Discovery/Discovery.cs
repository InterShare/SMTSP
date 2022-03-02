using System.Collections.ObjectModel;
using System.Net.NetworkInformation;
using Makaretu.Dns;
using MDNS;
using SMTSP.Core;
using SMTSP.Entities;

namespace SMTSP.Discovery;

/// <summary>
/// Used to discover devices in the current network.
/// </summary>
public class Discovery : IDisposable
{
    private readonly DeviceInfo _myDevice;
    private readonly ServiceDiscovery _serviceDiscovery;

    /// <summary>
    /// Holds the list of discovered devices.
    /// </summary>
    public readonly ObservableCollection<DeviceInfo> DiscoveredDevices = new ObservableCollection<DeviceInfo>();

    /// <param name="myDevice">The current device, used to advertise on the network, so that other devices can find this one</param>
    public Discovery(DeviceInfo myDevice)
    {
        _myDevice = myDevice;
        _serviceDiscovery = new ServiceDiscovery();

        _serviceDiscovery.ServiceInstanceDiscovered += OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown += OnServiceInstanceShutdown;
        _serviceDiscovery.Mdns.AnswerReceived += OnAnswerReceived;

        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
    }

    private void OnAnswerReceived(object? sender, MessageEventArgs args)
    {
        TXTRecord? txtRecord = args.Message.Answers.OfType<TXTRecord>().FirstOrDefault();

        if (txtRecord != null && txtRecord.Name.ToString().Contains(SmtsConfig.ServiceName) && !txtRecord.Name.ToString().StartsWith(_myDevice.DeviceId))
        {
            SRVRecord? srvRecord = args.Message.Answers.OfType<SRVRecord>().FirstOrDefault();

            if (srvRecord != null)
            {
                GetDeviceFromRecords(txtRecord, srvRecord, args.RemoteEndPoint?.Address?.ToString() ?? "");
            }
        }
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        DiscoveredDevices.Clear();
        // Advertise();
        SendOutLookupSignal();
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

    private void GetDeviceFromRecords(TXTRecord record, SRVRecord srvRecord, string ipAddress)
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


            if (string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(deviceName) || string.IsNullOrEmpty(deviceType) || string.IsNullOrEmpty(protocolVersionString) || srvRecord?.Port == null)
            {
                return;
            }

            ushort protocolVersion = ushort.Parse(protocolVersionString);

            lock (DiscoveredDevices)
            {
                DeviceInfo? existingDevice = DiscoveredDevices.FirstOrDefault(element => element.DeviceId == deviceId);

                if (existingDevice == null)
                {
                    DiscoveredDevices.Add(new DeviceInfo
                    {
                        DeviceId = deviceId,
                        DeviceName = deviceName,
                        DeviceType = deviceType,
                        IpAddress = ipAddress,
                        Port = srvRecord.Port,
                        ProtocolVersionIncompatible = protocolVersion != SmtsConfig.ProtocolVersion
                    });
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private void OnServiceInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs eventArgs)
    {
        try
        {
            if (eventArgs.ServiceInstanceName.ToString().Contains(SmtsConfig.ServiceName) && !eventArgs.ServiceInstanceName.ToString().StartsWith(_myDevice.DeviceId))
            {
                TXTRecord? txtRecord = eventArgs.Message.Answers.OfType<TXTRecord>().FirstOrDefault();
                SRVRecord? srvRecord = eventArgs.Message.Answers.OfType<SRVRecord>().FirstOrDefault();

                if (txtRecord == null || srvRecord == null)
                {
                    var service = eventArgs.ServiceInstanceName.ToString();
                    var query = new Message();
                    query.Questions.Add(new Question { Name = service, Type = DnsType.SRV });
                    query.Questions.Add(new Question { Name = service, Type = DnsType.TXT });
                    _serviceDiscovery.Mdns.SendQuery(query);

                    return;
                }

                GetDeviceFromRecords(txtRecord, srvRecord, eventArgs.RemoteEndPoint?.Address?.ToString() ?? "");
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
        }
    }

    private void OnServiceInstanceShutdown(object? sender, ServiceInstanceShutdownEventArgs eventArgs)
    {
        if (eventArgs.ServiceInstanceName.ToString().Contains(SmtsConfig.ServiceName))
        {
            lock (DiscoveredDevices)
            {
                DeviceInfo? existingDevice = DiscoveredDevices.FirstOrDefault(element => element.IpAddress == eventArgs.RemoteEndPoint?.Address?.ToString());

                if (existingDevice != null)
                {
                    DiscoveredDevices.Remove(existingDevice);
                }
            }
        }
    }

    /// <summary>
    /// Sends a signal, to queries all devices found on the network.
    /// </summary>
    public void SendOutLookupSignal()
    {
        const string service = "_smtsp._tcp.local";
        var query = new Message();
        query.Questions.Add(new Question { Name = service, Type = DnsType.PTR });

        _serviceDiscovery.Mdns.SendQuery(query);
    }

    /// <summary>
    /// Disposes this instance.
    /// </summary>
    public void Dispose()
    {
        _serviceDiscovery.ServiceInstanceDiscovered -= OnServiceInstanceDiscovered;
        _serviceDiscovery.ServiceInstanceShutdown -= OnServiceInstanceShutdown;
        _serviceDiscovery.Mdns.AnswerReceived -= OnAnswerReceived;
        NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
        _serviceDiscovery.Dispose();
    }
}