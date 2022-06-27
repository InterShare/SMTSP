using NUnit.Framework;
using SMTSP.Discovery.Entities;
using SMTSP.Entities;
using SMTSP.Entities.Content;

namespace SMTSP.Test;

public class TransferTest
{
    private readonly DeviceInfo _sender = new DeviceInfo("05DD541B-B351-4EE3-9BA5-1F9663E0FC4B", "TestDevice 1", 42003, DeviceTypes.Computer, "127.0.0.1");
    private readonly DeviceInfo _receiver = new DeviceInfo("EE27A6ED-6F30-4299-A35F-AC3B7139F733", "TestDevice 2", 42013, DeviceTypes.Phone, "127.0.0.1");

    [SetUp]
    public void SetUp()
    {
        var thread = new Thread(RunReceiver);
        thread.Start();
        // RunReceiver();
    }

    private void RunReceiver()
    {
        var receiver = new SmtspReceiver();
        receiver.StartReceiving();
        _receiver.Port = receiver.Port;
        receiver.RegisterTransferRequestCallback(delegate(TransferRequest request)
        {
            Assert.Multiple(() =>
            {
                Assert.That(request.SessionPublicKey!, Is.Not.Empty);
                Assert.That(_sender.DeviceId, Is.EqualTo(request.SenderId));
            });

            return Task.FromResult(true);
        });
    }

    [Test]
    public async Task FileTransfer()
    {
        using var fileContent = new MemoryStream();
        await using var writer = new StreamWriter(fileContent);
        await writer.WriteLineAsync("Hello, World");
        await writer.FlushAsync();
        fileContent.Position = 0;

        var content = new SmtspFileContent();
        content.FileName = "SomeFile.txt";
        content.DataStream = fileContent;

        await SmtspSender.SendFile(_receiver, content, _sender);
    }
}