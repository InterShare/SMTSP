using System.Net;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using SMTSP.Discovery;

namespace SMTSP.Test;

public class DiscoveryTests
{
    private const string AdvertisedDeviceId = "8F596F84-57AD-4D97-817D-D5ADECD2A9FF";
    private readonly X509Certificate2 _certificate = EncryptionHelper.GenerateSelfSignedCertificate();

    [SetUp]
    public void Setup()
    {
        var discovery = new NearbyCommunication(new Device
        {
            Name = "Advertised [TEST]",
            Id = AdvertisedDeviceId,
            Type = Device.Types.DeviceType.Mobile,
            TcpConnectionInfo = new TcpConnectionInfo
            {
                IpAddress = IPAddress.Loopback.ToString(),
                Port = 42420
            }
        }, _certificate);

        discovery.AdvertiseDevice();
    }

    [Test]
    public void TestBonjourDiscovery()
    {
        var discovery = new DeviceDiscovery(new Device
        {
            Name = "Discovery [TEST]",
            Id = Guid.NewGuid().ToString(),
            Type = Device.Types.DeviceType.Mobile
        });

        discovery.Browse();

        Device? foundDevice = null;

        for (var i = 0; i < 30; i++)
        {
            foundDevice = discovery.DiscoveredDevices.FirstOrDefault(device => device.Id == AdvertisedDeviceId);

            if (foundDevice != null)
            {
                break;
            }

            Thread.Sleep(1000);
        }

        if (foundDevice == null)
        {
            Assert.Fail($"Expected to find device {AdvertisedDeviceId}");
        }
    }
}
