using Google.Protobuf;
using SMTSP.Discovery;

namespace SMTSP.Communication.TransferTypes;

public record TransferBase(Device Sender, Stream _encryptedStream)
{
    private readonly Stream _encryptedStream = _encryptedStream;

    public async Task AcceptAsync()
    {
        await _encryptedStream.WriteAsync(new TransferRequestResponse
        {
            Answer = TransferRequestResponse.Types.Answers.Accept
        }.ToByteArray());
    }

    public async Task DenyAsync()
    {
        await _encryptedStream.WriteAsync(new TransferRequestResponse
        {
            Answer = TransferRequestResponse.Types.Answers.Deny
        }.ToByteArray());
    }

    public void Accept()
    {
        _encryptedStream.Write(new TransferRequestResponse
        {
            Answer = TransferRequestResponse.Types.Answers.Accept
        }.ToByteArray());
    }

    public void Deny()
    {
        _encryptedStream.Write(new TransferRequestResponse
        {
            Answer = TransferRequestResponse.Types.Answers.Deny
        }.ToByteArray());
    }
}
