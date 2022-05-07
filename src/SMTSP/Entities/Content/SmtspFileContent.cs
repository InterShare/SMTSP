using SMTSP.Core;
using SMTSP.Extensions;

namespace SMTSP.Entities.Content;

/// <summary>
/// Data will be interpreted as file
/// </summary>
[SmtsContent("FileContent")]
public class SmtspFileContent : SmtspContent
{
    /// <summary>
    /// File Name
    /// </summary>
    [IncludeInBody]
    public string? FileName { get; set; }

    /// <summary>
    /// The size of the File
    /// </summary>
    [IncludeInBody]
    public long FileSize { get; set; }
}