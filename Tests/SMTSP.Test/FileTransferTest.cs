using System.Net;
using System.Security.Cryptography.X509Certificates;
using NUnit.Framework;
using SMTSP.Communication;
using SMTSP.Communication.TransferTypes;
using SMTSP.Discovery;

namespace SMTSP.Test;

public class FileTransferTest
{
    private const string ReceivedFilePath = "./ReceivedFile.txt";
    private const string FileContent = "Hello, World\n";

    private readonly X509Certificate2 _certificate = EncryptionHelper.GenerateSelfSignedCertificate();

    private static readonly Device ServerDevice = new()
    {
        Name = "Server [TEST]",
        Id = "1",
        Type = Device.Types.DeviceType.Mobile,
        TcpConnectionInfo = new TcpConnectionInfo
        {
            Hostname = Dns.GetHostName()
        }
    };

    private async void StartServer(TaskCompletionSource<bool> completionHandlerSource)
    {
        var nearbyServer = new NearbyCommunication(ServerDevice, _certificate);

        nearbyServer.OnConnectionRequest += (_, transferRequest) =>
        {
            try
            {
                transferRequest.Accept();

                if (transferRequest is FileTransfer fileTransfer)
                {
                    var fileStream = fileTransfer.GetFile();

                    using var newFile = File.OpenWrite(ReceivedFilePath);
                    fileStream.CopyTo(newFile);
                    newFile.Close();

                    var fileContent = File.ReadAllText(ReceivedFilePath);

                    completionHandlerSource.SetResult(fileContent == FileContent);

                    return;
                }

                completionHandlerSource.SetResult(false);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                completionHandlerSource.SetResult(false);
            }
        };

        await nearbyServer.StartReceiving();
    }

    [Test]
    public async Task TestFileTransfer()
    {
        var completionHandlerSource = new TaskCompletionSource<bool>();

        StartServer(completionHandlerSource);

        var device = new Device
        {
            Name = "Sender [TEST]",
            Id = "2",
            Type = Device.Types.DeviceType.Mobile
        };

        var nearby = new NearbyCommunication(device, _certificate);

        var fileStream = File.OpenRead("./TestFile.txt");
        var fileInfo = new SharedFileInfo
        {
            FileName = "TestFile.txt",
            FileSize = fileStream.Length
        };

        await nearby.SendFile(ServerDevice, fileInfo, fileStream);

        var result = await completionHandlerSource.Task;

        Assert.That(result, Is.True);
    }
}
