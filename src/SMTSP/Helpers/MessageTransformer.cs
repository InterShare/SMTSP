using SMTSP.Entities;
using SMTSP.Extensions;

namespace SMTSP.Helpers;

public class MessageTransformer
{
    public static GetMessageTypeResponse GetMessageType(Stream stream)
    {
        var result = new GetMessageTypeResponse
        {
            Version = stream.GetStringTillEndByte(0x00)
        };

        string type = stream.GetStringTillEndByte(0x00);
        result.Type = type.ToEnum<MessageTypes>(MessageTypes.Unknown);

        return result;
    }
}