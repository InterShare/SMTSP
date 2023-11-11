using Google.Protobuf;
using SMTSP.Discovery;

namespace SMTSP.Communication.TransferTypes;

public record TransferBase(Device Sender, Stream _encryptedStream)
{
    private readonly Stream _encryptedStream = _encryptedStream;

    public void Accept()
    {
        new TransferRequestResponse
        {
            Answer = TransferRequestResponse.Types.Answers.Accept
        }.WriteDelimitedTo(_encryptedStream);
    }

    public void Deny()
    {
        new TransferRequestResponse
        {
            Answer = TransferRequestResponse.Types.Answers.Deny
        }.WriteDelimitedTo(_encryptedStream);
    }
}
