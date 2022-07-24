using SMTSP.Entities;
using SMTSP.Extensions;

namespace SMTSP.Helpers;

internal class MessageTransformer
{
    internal static GetMessageTypeResponse? GetMessageType(Stream stream)
    {
        var properties = stream.GetProperties();

        string? version = properties.GetValueOrDefault("SmtspVersion");
        var messageType = properties.GetValueOrDefault("MessageType")?.ToEnum<MessageTypes>(MessageTypes.Unknown);

        if (!string.IsNullOrEmpty(version) && messageType != null)
        {
            return new GetMessageTypeResponse(version, (MessageTypes) messageType);
        }

        return null;
    }
}