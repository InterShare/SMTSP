namespace SMTSP.Entities;

public class GetMessageTypeResponse
{
    public string Version { get; set; }
    public MessageTypes Type { get; set; }
    public long NewStartPosition { get; set; }
}