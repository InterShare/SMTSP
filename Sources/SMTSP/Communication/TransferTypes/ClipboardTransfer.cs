using SMTSP.Discovery;

namespace SMTSP.Communication.TransferTypes;

public record ClipboardTransfer(Device Sender, Stream _encryptedStream, string Content) : TransferBase(Sender, _encryptedStream)
{
    private Stream _encryptedStream = _encryptedStream;
}
