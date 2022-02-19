using SMTSP.Core;
using SMTSP.Extensions;

namespace SMTSP.Entities;

public class TransferRequest
{
    public string Id { get; set; }
    public string SenderId { get; set; }
    public string SenderName { get; set; }
    public long FileSize { get; set; }
    public string FileName { get; set; }

    public byte[] ToBinary()
    {
        var messageInBytes = new List<byte>();

        messageInBytes.AddSmtsHeader(MessageTypes.TransferRequest);

        Logger.Info($"Id: {Id}");
        messageInBytes.AddRange(Id.GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"SenderId: {SenderId}");
        messageInBytes.AddRange(SenderId.GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"SenderName: {SenderName}");
        messageInBytes.AddRange(SenderName.GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"FileName: {FileName}");
        messageInBytes.AddRange(FileName.GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"FileSize: {FileSize}");
        messageInBytes.AddRange(FileSize.ToString().GetBytes());
        messageInBytes.Add(0x00);

        return messageInBytes.ToArray();
    }

    public void FromStream(Stream stream)
    {
        Id = stream.GetStringTillEndByte(0x00);
        SenderId = stream.GetStringTillEndByte(0x00);
        SenderName = stream.GetStringTillEndByte(0x00);
        FileName = stream.GetStringTillEndByte(0x00);

        string fileSize = stream.GetStringTillEndByte(0x00);

        if (fileSize != null)
        {
            FileSize = long.Parse(fileSize);
        }
    }
}