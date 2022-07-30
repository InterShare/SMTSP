using SMTSP.Core;
using SMTSP.Entities.Content;
using SMTSP.Extensions;

namespace SMTSP.Entities;

/// <summary>
/// Pretty self explaining, I think.
/// </summary>
public class TransferRequest
{
    /// <summary>
    /// The identifier associated with the request.
    /// </summary>
    public string Id { get; set; } = null!;

    /// <summary>
    /// Identifier of the one, who requested a file transfer.
    /// </summary>
    public string SenderId { get; set; } = null!;


    /// <summary>
    /// The name of the one, who requested a file transfer.
    /// </summary>
    public string SenderName { get; set; } = null!;

    /// <summary>
    ///
    /// </summary>
    public SmtspContentBase ContentBase { get; set; } = null!;

    /// <summary>
    /// The session public key for this transfer.
    /// </summary>
    public byte[]? SessionPublicKey { get; set; }

    /// <param name="id"></param>
    /// <param name="senderId"></param>
    /// <param name="senderName"></param>
    /// <param name="contentBase"></param>
    /// <param name="sessionPublicKey"></param>
    public TransferRequest(string id, string senderId, string senderName, SmtspContentBase contentBase, byte[]? sessionPublicKey)
    {
        Id = id;
        SenderId = senderId;
        SenderName = senderName;
        ContentBase = contentBase;
        SessionPublicKey = sessionPublicKey;
    }

    internal TransferRequest(Stream stream)
    {
        FromStream(stream);
    }

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

        Logger.Info($"Attaching PublicKey");
        messageInBytes.AddRange(SessionPublicKey!);

        messageInBytes.AddRange(ContentBase.ToBinary());
        messageInBytes.Add(0x00);

        return messageInBytes.ToArray();
    }

    internal static Type? FindContentImplementation(string contentType)
    {
        switch (contentType)
        {
            // TODO: make this more robust
            case "FileContent":
                return typeof(SmtspFileContent);
            case "RawContent":
                return typeof(SmtspRawContent);
            default:
            {
                var list = from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    let attributes = type.GetCustomAttributes(typeof(SmtspContentAttribute), true)
                    where attributes is { Length: > 0 }
                    where attributes.Cast<SmtspContentAttribute>().First().Name == contentType
                    select type;

                return list.FirstOrDefault();
            }
        }
    }

    internal void FromStream(Stream stream)
    {
        Id = stream.GetStringTillEndByte(0x00);
        SenderId = stream.GetStringTillEndByte(0x00);
        SenderName = stream.GetStringTillEndByte(0x00);
        string contentType = stream.GetStringTillEndByte(0x00);

        byte[] publicKey = new byte[67];
        int read = stream.Read(publicKey, 0, publicKey.Length);

        if (read != publicKey.Length)
        {
            throw new Exception("Fever bytes read than expected when trying to get public key.");
        }

        SessionPublicKey = publicKey;

        Type? type = FindContentImplementation(contentType);

        if (type != null)
        {
            if (Activator.CreateInstance(type) is SmtspContentBase contentBase)
            {
                contentBase.FromStream(stream);
                ContentBase = contentBase;
            }
        }
    }
}