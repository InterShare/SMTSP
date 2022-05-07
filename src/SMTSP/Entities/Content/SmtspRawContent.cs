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
    [IncludeInBody]
    public string ContentType { get; set; }
}