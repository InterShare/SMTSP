using SMTSP.Core;
using SMTSP.Entities;

namespace SMTSP.Extensions;

internal static class ByteListExtension
{
    internal static void AddSmtsHeader(this List<byte> bytes, MessageTypes messageType)
    {
        bytes.AddRange($"SmtspVersion={SmtsConfiguration.ProtocolVersion.ToString()}".GetBytes());
        bytes.Add(0x00);

        bytes.AddRange($"MessageType={messageType.ToString()}".GetBytes());
        bytes.Add(0x00);
    }

    internal static void AddProperty(this List<byte> bytes, string propertyName, string propertyValue)
    {
        bytes.AddRange($"{propertyName}={propertyValue}".GetBytes());
        bytes.Add(0x00);
    }

    internal static string GetStringTillEndByte(this IEnumerable<byte> bytes, byte endByte, ref int startPosition)
    {
        var skipped = bytes.Skip(startPosition);
        byte[] result = skipped.TakeWhile(bytePart => bytePart != endByte).ToArray();

        startPosition += result.Length + 1;

        return result.Any() ? result.GetStringFromBytes() : "";
    }
}