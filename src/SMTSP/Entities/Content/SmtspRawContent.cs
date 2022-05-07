using SMTSP.Core;
using SMTSP.Extensions;

namespace SMTSP.Entities.Content;

/// <summary>
/// Data will be interpreted as clipboard
/// </summary>
[SmtsContent("RawContent")]
public class SmtspRawContent : SmtspContent
{
    /// <summary>
    /// Contains a type description for the data.
    /// </summary>
    public string ContentType { get; set; }

    internal IEnumerable<byte> ToBinary()
    {
        var messageInBytes = new List<byte>();

        Logger.Info($"FileName: {ContentType}");
        messageInBytes.AddRange(ContentType.GetBytes());
        messageInBytes.Add(0x00);

        return messageInBytes;
    }

    internal void FromStream(Stream stream)
    {
        ContentType = stream.GetStringTillEndByte(0x00);
    }
}