using System.Text;
using SMTSP.Core;
using SMTSP.Entities.Content;
using SMTSP.Extensions;

namespace SMTSP.Entities;

/// <summary>
/// Pretty self explaining, I think.
/// </summary>
public class TransferRequest
{
    public string Id { get; set; }
    public string SenderId { get; set; }
    public string SenderName { get; set; }
    public SmtspContentBase ContentBase { get; set; }
    public byte[]? PublicKey { get; set; }

    internal byte[] ToBinary()
    {
        var messageInBytes = new List<byte>();

        messageInBytes.AddSmtsHeader(MessageTypes.TransferRequest);

        Logger.Info($"Id: {Id}");
        messageInBytes.AddRange(Id.GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"SenderId: {SenderId}");
        messageInBytes.AddRange(SenderId.GetBytes());
        messageInBytes.Add(0x00);

        Logger.Info($"SenderName: {SenderName}");
        messageInBytes.AddRange(SenderName.GetBytes());
        messageInBytes.Add(0x00);

        string attributeName = "";

        if (ContentBase.GetType().GetCustomAttributes(typeof(SmtspContentAttribute), true).FirstOrDefault() is SmtspContentAttribute attribute)
        {
            attributeName = attribute.Name;
        }

        Logger.Info($"ContentType: {attributeName}");
        messageInBytes.AddRange(attributeName.GetBytes());
        messageInBytes.Add(0x00);

        string publicKeyInBase64 = PublicKeyToBase64(PublicKey!);

        Logger.Info($"PublicKey: {publicKeyInBase64}");
        messageInBytes.AddRange(PublicKey!);
        // messageInBytes.Add(0x00);

        messageInBytes.AddRange(ContentBase.ToBinary());
        messageInBytes.Add(0x00);

        return messageInBytes.ToArray();
    }

    internal static Type? FindContentImplementation(string contentType)
    {
        var list = from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            let attributes = type.GetCustomAttributes(typeof(SmtspContentAttribute), true)
            where attributes is { Length: > 0 }
            where attributes.Cast<SmtspContentAttribute>().First().Name == contentType
            select type;

        return list?.FirstOrDefault();
    }

    internal static byte[] GetPublicKey(Stream stream)
    {
        return stream.GetStringTillEndByte(0x00).GetBytes().ToArray();
        return Convert.FromBase64String(stream.GetStringTillEndByte(0x00));
    }

    internal static string PublicKeyToBase64(byte[] key)
    {
        return key.GetStringFromBytes();
        return Convert.ToBase64String(key);
    }

    internal void FromStream(Stream stream)
    {
        Id = stream.GetStringTillEndByte(0x00);
        SenderId = stream.GetStringTillEndByte(0x00);
        SenderName = stream.GetStringTillEndByte(0x00);
        string contentType = stream.GetStringTillEndByte(0x00);
        // PublicKey = GetPublicKey(stream);

        byte[] publicKey = new byte[67];
        stream.Read(publicKey, 0, publicKey.Length);
        PublicKey = publicKey;

        Type? type = FindContentImplementation(contentType);

        if (type != null)
        {
            var contentBase = (SmtspContentBase) Activator.CreateInstance(type);
            contentBase.FromStream(stream);

            ContentBase = contentBase;
        }
    }
}