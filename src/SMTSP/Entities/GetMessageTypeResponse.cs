namespace SMTSP.Entities;

internal class GetMessageTypeResponse
{
    public string Version { get; }
    public MessageTypes Type { get; }

    public GetMessageTypeResponse(string version, MessageTypes type)
    {
        Version = version;
        Type = type;
    }
}