namespace SMTSP.Entities;

public class LanDeviceInfo
{
    public string Ip { get; set; }
    public int DiscoveryPort { get; set; }
    public string[] AcceptFiles { get; set; }
    public int FileServerPort { get; set; }
}