using NUnit.Framework;
using SMTSP.Discovery;
using SMTSP.Discovery.Entities;
using SMTSP.Entities;

namespace SMTSP.Test;

public class DiscoveryTest
{
    private DeviceDiscovery _firstDeviceAdvertiser = null!;
    private DeviceDiscovery _secondDeviceDiscovery = null!;

    private DeviceInfo _firstDevice = null!;
    private DeviceInfo _secondDevice = null!;

    [SetUp]
    public void Setup()
    {
        var thread = new Thread(RunAdvertiser);
        thread.Start();

        _secondDevice = new DeviceInfo("EE27A6ED-6F30-4299-A35F-AC3B7139F733", "TestDevice 2", 42013, DeviceTypes.Phone, "192.168.1.43", new[] { Capabilities.FileTransfer });
        _secondDeviceDiscovery = new DeviceDiscovery(_secondDevice);
    }

    private void RunAdvertiser()
    {
        _firstDevice = new DeviceInfo("05DD541B-B351-4EE3-9BA5-1F9663E0FC4B", "TestDevice 1", 42003, DeviceTypes.Computer, "192.168.1.42", new[] { Capabilities.FileTransfer });
        _firstDeviceAdvertiser = new DeviceDiscovery(_firstDevice);
        _firstDeviceAdvertiser.Advertise();
    }

    [Test]
    public void DiscoverDevice()
    {
        _secondDeviceDiscovery.StartDiscovering();

        DeviceInfo? foundDevice = null;

        for (int i = 0; i < 10; i++)
        {
            foundDevice = _secondDeviceDiscovery.DiscoveredDevices.FirstOrDefault(device => device.DeviceId == _firstDevice.DeviceId);

            if (foundDevice?.DeviceId == _firstDevice.DeviceId)
            {
                break;
            }

            Thread.Sleep(1000);
        }

        if (foundDevice == null)
        {
            Assert.Fail($"Expected to find device {_firstDevice.DeviceId}");
        }
    }
}