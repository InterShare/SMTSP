using SMTSP.Entities;
using SMTSP.Extensions;

namespace SMTSP.Helpers;

internal class MessageTransformer
{
    internal static GetMessageTypeResponse? GetMessageType(Stream stream)
    {
        string? version = stream.GetProperty("SmtspVersion");
        var messageType = stream.GetProperty("MessageType")?.ToEnum<MessageTypes>(MessageTypes.Unknown);

        if (!string.IsNullOrEmpty(version) && messageType != null)
        {
            return new GetMessageTypeResponse(version, (MessageTypes) messageType);
        }

        return null;
    }
}