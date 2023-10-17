using System.IO.Compression;
using SMTSP.Discovery;

namespace SMTSP.Communication.TransferTypes;

public record FileTransfer(Device Sender, Stream _encryptedStream, SharedFileInfo[] FileInfos) : TransferBase(Sender, _encryptedStream)
{
    private Stream _encryptedStream = _encryptedStream;

    public Stream GetFile()
    {
        return _encryptedStream;
    }

    public ZipArchive GetFiles()
    {
        var archive = new ZipArchive(_encryptedStream, ZipArchiveMode.Read, false);
        return archive;
    }
}
