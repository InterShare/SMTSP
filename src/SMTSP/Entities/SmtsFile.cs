namespace SMTSP.Entities;

public class SmtsFile
{
    public string Name { get; set; }
    public Stream DataStream { get; set; }
    public long FileSize { get; set; }
    public string FilePath { get; set; }
}