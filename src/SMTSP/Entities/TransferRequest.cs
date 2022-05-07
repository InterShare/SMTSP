using SMTSP.Core;
using SMTSP.Entities.Content;
using SMTSP.Extensions;

namespace SMTSP.Entities;

/// <summary>
/// </summary>
public class TransferRequest
{
    /// <summary>
    ///
    /// </summary>
    public string Id { get; set; }
    public string SenderId { get; set; }
    public string SenderName { get; set; }
    // public long FileSize { get; set; }
    // public string FileName { get; set; }
    public SmtspContent Content { get; set; }

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

        if (Content.GetType().GetCustomAttributes(typeof(SmtsContentAttribute), true).FirstOrDefault() is SmtsContentAttribute attribute)
        {
            attributeName = attribute.Name;
        }

        Logger.Info($"ContentType: {attributeName}");
        messageInBytes.AddRange(attributeName.GetBytes());
        messageInBytes.Add(0x00);

        // Logger.Info($"FileName: {FileName}");
        // messageInBytes.AddRange(FileName.GetBytes());
        // messageInBytes.Add(0x00);
        //
        // Logger.Info($"FileSize: {FileSize}");
        // messageInBytes.AddRange(FileSize.ToString().GetBytes());
        // messageInBytes.Add(0x00);

        messageInBytes.AddRange(Content.ToBinary());

        return messageInBytes.ToArray();
    }

    internal static Type? FindContentImplementation(string contentType)
    {
        var list = from assembly in AppDomain.CurrentDomain.GetAssemblies()
            from type in assembly.GetTypes()
            let attributes = type.GetCustomAttributes(typeof(SmtsContentAttribute), true)
            where attributes is { Length: > 0 }
            where attributes.Cast<SmtsContentAttribute>().First().Name == contentType
            select type;

        return list?.FirstOrDefault();
    }

    internal void FromStream(Stream stream)
    {
        Id = stream.GetStringTillEndByte(0x00);
        SenderId = stream.GetStringTillEndByte(0x00);
        SenderName = stream.GetStringTillEndByte(0x00);
        string contentType = stream.GetStringTillEndByte(0x00);

        Type? type = FindContentImplementation(contentType);

        if (type != null)
        {
            SmtspContent content = (SmtspContent) Activator.CreateInstance(type);
            content.FromStream(stream);

            Content = content;
        }
    }
}