using SMTSP.Core;
using SMTSP.Entities;

namespace SMTSP.Extensions;

internal static class ByteListExtension
{
    public static void AddSmtsHeader(this List<byte> bytes, MessageTypes messageType)
    {
        Logger.Info($"Protocol Version: {SmtsConfig.ProtocolVersion}");
        bytes.AddRange(SmtsConfig.ProtocolVersion.GetBytes());
        bytes.Add(0x00);

        string messageTypeString = messageType.ToLowerCamelCaseString();

        Logger.Info($"Message type: {messageTypeString}");
        bytes.AddRange(messageTypeString.GetBytes());
        bytes.Add(0x00);
    }

    public static string GetStringTillEndByte(this IEnumerable<byte> bytes, byte endByte, ref int startPosition)
    {
        IEnumerable<byte> skipped = bytes.Skip(startPosition);
        byte[] result = skipped.TakeWhile(bytePart => bytePart != endByte).ToArray();

        startPosition += result.Length + 1;

        return result.Any() ? result.GetStringFromBytes() : "";
    }
}